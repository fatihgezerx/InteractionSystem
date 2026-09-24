using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InteractionSystem
{
    /// <summary>
    /// Static entry point of the interaction system. Call <see cref="Initialize"/> once (e.g. from a
    /// GameManager) with an <see cref="InteractData"/>; from then on detection runs on a cancellable
    /// UniTask loop - no <c>Update</c>, no coroutine - and publishes
    /// <see cref="EventTypes.OnFocus"/>, <see cref="EventTypes.OnLoseFocus"/> and
    /// <see cref="EventTypes.OnInteract"/> (plus <see cref="EventTypes.OnInteracting"/> while a Holding
    /// object is being held) through <see cref="EventManager"/>, alongside the focused
    /// <see cref="Interactable"/>'s own UnityEvents.
    /// </summary>
    /// <remarks>
    /// Input comes from the <c>Interact</c> action of the project-wide Input System actions
    /// (Project Settings &gt; Input System Package). <see cref="EventTypes.OnFocus"/> fires once per
    /// object until it loses focus: re-detecting the same object never re-focuses it, so it never
    /// restarts a hold in progress.
    /// </remarks>
    public static class InteractionManager
    {
        private const string InteractActionName = "Interact";

        private static readonly RaycastHit[] LineHits = new RaycastHit[32];
        private static readonly Collider[] SphereHits = new Collider[32];

        private static InteractData _data;
        private static Transform _originRoot;
        private static Transform _origin;
        private static InputAction _interactAction;
        private static CancellationTokenSource _loopCts;
        private static CancellationTokenSource _holdCts;
        private static Interactable _holdTarget;

        // OnInteracting fires every frame of a hold, so its args are reused instead of allocated.
        private static readonly InteractionArgs InteractingArgs = new(null);

        /// <summary>Whether <see cref="Initialize"/> has been called and detection is running.</summary>
        public static bool IsInitialized => _loopCts != null;

        /// <summary>The data detection runs with, or null.</summary>
        public static InteractData Data => _data;

        /// <summary>The object detection is attached to (the tagged object, or its child when In Child is on), or null.</summary>
        public static Transform Origin => _origin;

        /// <summary>The tagged object itself, or null.</summary>
        public static Transform OriginRoot => _originRoot;

        /// <summary>Where detection starts: <see cref="Origin"/> offset by the data's Local Position. Valid while initialized.</summary>
        public static Vector3 RayOrigin => _origin.TransformPoint(_data.Settings.LocalPosition);

        /// <summary>Which way the line points: <see cref="Origin"/>'s forward turned by the data's Local Rotation. Valid while initialized.</summary>
        public static Vector3 RayDirection => _origin.rotation * Quaternion.Euler(_data.Settings.LocalRotation) * Vector3.forward;

        /// <summary>The interactable currently focused, or null.</summary>
        public static Interactable Current { get; private set; }

        /// <summary>
        /// Resolves the origin from <paramref name="data"/> (tagged object, optionally one of its
        /// children), hooks up the <c>Interact</c> input action, and starts detection.
        /// Calling it again restarts the system with the new data.
        /// </summary>
        public static void Initialize(InteractData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            Shutdown();

            var tagged = GameObject.FindWithTag(data.Settings.OriginTag);
            if (tagged == null)
            {
                Debug.LogError($"[InteractionManager] No GameObject tagged '{data.Settings.OriginTag}' found for the origin.");
                return;
            }

            var origin = tagged.transform;
            if (data.Settings.InChild)
            {
                if (data.Settings.ChildCount >= origin.childCount)
                {
                    Debug.LogError($"[InteractionManager] '{tagged.name}' has no child at index {data.Settings.ChildCount}.", tagged);
                    return;
                }

                origin = origin.GetChild(data.Settings.ChildCount);
            }

            _interactAction = InputSystem.actions != null ? InputSystem.actions.FindAction(InteractActionName) : null;
            if (_interactAction == null)
            {
                Debug.LogError($"[InteractionManager] No '{InteractActionName}' action found in the project-wide " +
                                "input actions. Add one (see README_INTERACTION.md).");
                return;
            }

            _data = data;
            _originRoot = tagged.transform;
            _origin = origin;
            _interactAction.started += OnInteractPressed;
            _interactAction.canceled += OnInteractReleased;
            _interactAction.Enable();

            _loopCts = new CancellationTokenSource();
            DetectLoopAsync(_loopCts.Token).Forget();
        }

        /// <summary>Stops detection, unhooks the input and drops the current focus.</summary>
        public static void Shutdown()
        {
            if (_interactAction != null)
            {
                _interactAction.started -= OnInteractPressed;
                _interactAction.canceled -= OnInteractReleased;
                _interactAction = null;
            }

            if (_loopCts != null)
            {
                _loopCts.Cancel();
                _loopCts.Dispose();
                _loopCts = null;
            }

            SetCurrent(null);
            _data = null;
            _originRoot = null;
            _origin = null;
        }

        private static async UniTaskVoid DetectLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // The origin was destroyed (e.g. its scene unloaded) - stop until initialized again.
                if (_origin == null)
                {
                    Shutdown();
                    return;
                }

                SetCurrent(_data.Settings.RaycastType == RaycastType.Sphere ? DetectSphere() : DetectLine());

                var canceled = _data.Settings.CheckInterval > 0f
                    ? await UniTask.Delay(TimeSpan.FromSeconds(_data.Settings.CheckInterval), cancellationToken: token).SuppressCancellationThrow()
                    : await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow();

                if (canceled)
                {
                    return;
                }
            }
        }

        // The ray is cast against every layer so that whatever is in front can block it: only if the
        // first thing it hits is on Raycast Layers is that object detected. Triggers outside Raycast
        // Layers (invisible zones) don't block.
        private static Interactable DetectLine()
        {
            var count = Physics.RaycastNonAlloc(RayOrigin, RayDirection, LineHits, _data.Settings.Radius,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);

            Collider nearest = null;
            var nearestDistance = float.MaxValue;

            for (var i = 0; i < count; i++)
            {
                var hitCollider = LineHits[i].collider;
                if (IsOwnCollider(hitCollider) || (hitCollider.isTrigger && !IsOnRaycastLayers(hitCollider)))
                {
                    continue;
                }

                if (LineHits[i].distance < nearestDistance)
                {
                    nearest = hitCollider;
                    nearestDistance = LineHits[i].distance;
                }
            }

            return nearest != null && IsOnRaycastLayers(nearest) ? ResolveInteractable(nearest) : null;
        }

        // Everything on Raycast Layers inside the sphere is a candidate, walls or not; the closest wins.
        private static Interactable DetectSphere()
        {
            var center = RayOrigin;
            var count = Physics.OverlapSphereNonAlloc(center, _data.Settings.Radius, SphereHits, _data.Settings.RaycastLayers,
                QueryTriggerInteraction.Collide);

            Interactable best = null;
            var bestSqrDistance = float.MaxValue;

            for (var i = 0; i < count; i++)
            {
                if (IsOwnCollider(SphereHits[i]))
                {
                    continue;
                }

                var candidate = ResolveInteractable(SphereHits[i]);
                if (candidate == null)
                {
                    continue;
                }

                var sqrDistance = (SphereHits[i].bounds.center - center).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    best = candidate;
                    bestSqrDistance = sqrDistance;
                }
            }

            return best;
        }

        private static bool IsOwnCollider(Collider collider) => collider.transform.IsChildOf(_originRoot);

        private static bool IsOnRaycastLayers(Collider collider) => (_data.Settings.RaycastLayers.value & (1 << collider.gameObject.layer)) != 0;

        private static Interactable ResolveInteractable(Collider collider)
        {
            var interactable = collider.GetComponentInParent<Interactable>();
            return interactable != null && interactable.isActiveAndEnabled ? interactable : null;
        }

        private static void SetCurrent(Interactable target)
        {
            // Same object as before: no OnFocus again, and a hold in progress keeps running.
            // ReferenceEquals, so a destroyed Current (== null in Unity terms) is still cleared.
            if (ReferenceEquals(Current, target))
            {
                return;
            }

            CancelHold();

            var previous = Current;
            Current = target;

            if (previous != null)
            {
                previous.LoseFocus();
                EventManager.InvokeEvent(EventTypes.OnLoseFocus, new InteractionArgs(previous));
            }

            if (target != null)
            {
                target.Focus();
                EventManager.InvokeEvent(EventTypes.OnFocus, new InteractionArgs(target));
            }
        }

        private static void OnInteractPressed(InputAction.CallbackContext context)
        {
            var target = Current;
            if (target == null)
            {
                return;
            }

            if (!target.Holding)
            {
                Interact(target);
                return;
            }

            CancelHold();
            _holdTarget = target;
            _holdCts = new CancellationTokenSource();
            PublishInteracting(target, 0f);
            HoldAsync(target, _holdCts).Forget();
        }

        private static void OnInteractReleased(InputAction.CallbackContext context) => CancelHold();

        // Counts the hold up frame by frame so OnInteracting can report it; interacts once Duration is reached.
        private static async UniTaskVoid HoldAsync(Interactable target, CancellationTokenSource cts)
        {
            var duration = target.HoldDuration;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                if (await UniTask.Yield(PlayerLoopTiming.Update, cts.Token).SuppressCancellationThrow())
                {
                    return;
                }

                // Destroyed while being held.
                if (target == null)
                {
                    CancelHold();
                    return;
                }

                elapsed = Mathf.Min(elapsed + Time.deltaTime, duration);
                PublishInteracting(target, elapsed);
            }

            _holdCts = null;
            _holdTarget = null;
            cts.Dispose();

            Interact(target);
            PublishInteracting(target, 0f);
        }

        /// <summary>Stops a hold in progress and reports it back to 0.</summary>
        private static void CancelHold()
        {
            if (_holdCts == null)
            {
                return;
            }

            _holdCts.Cancel();
            _holdCts.Dispose();
            _holdCts = null;

            var target = _holdTarget;
            _holdTarget = null;
            if (target != null)
            {
                PublishInteracting(target, 0f);
            }
        }

        private static void PublishInteracting(Interactable target, float elapsed)
        {
            InteractingArgs.Value = target;
            InteractingArgs.Elapsed = elapsed;
            EventManager.InvokeEvent(EventTypes.OnInteracting, InteractingArgs);
        }

        private static void Interact(Interactable target)
        {
            target.Interact();
            EventManager.InvokeEvent(EventTypes.OnInteract, new InteractionArgs(target));
        }

        // Keeps the static state clean when "Enter Play Mode Options" skips the domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _interactAction = null;
            _loopCts = null;
            _holdCts = null;
            _holdTarget = null;
            _data = null;
            _originRoot = null;
            _origin = null;
            Current = null;
        }
    }
}
