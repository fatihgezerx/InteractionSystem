using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

// Lets InteractData's Compile step write the hold settings without exposing public setters.
[assembly: InternalsVisibleTo("InteractionSystem.Editor")]

namespace InteractionSystem
{
    /// <summary>
    /// Makes a GameObject interactable. Added automatically (along with a <see cref="BoxCollider"/>
    /// if the object has no collider, and the <c>Interact</c> layer) by an <see cref="InteractData"/>'s
    /// "Compile" step. What happens on focus/interact is wired entirely from the Inspector through
    /// its <see cref="UnityEvent"/>s - e.g. to return a pooled object on interact, drag its
    /// <c>Poolable</c> into <c>On Interact</c> and pick <c>ReleaseSelf</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Interactable : MonoBehaviour
    {
        // Written by InteractData > Compile and hidden from the Inspector: InteractData is where these
        // are edited, so a value changed here would just be overwritten by the next Compile.
#if HAS_LOCALIZATION_SYSTEM
        // Offered for translation by LocalizationSystem's "Sync Project" (it scans prefab components),
        // as a whole sentence. The symbol is set only while LocalizationSystem is in the project, so the
        // attribute appears as soon as it is installed and nothing breaks without it.
        [LocalizationSystem.Localize]
#endif
        [HideInInspector] [SerializeField] internal string displayName;
        [HideInInspector] [SerializeField] internal bool holding;
        [HideInInspector] [SerializeField] internal float holdDuration = 1f;

        [Tooltip("Invoked when the raycast starts pointing at this object.")]
        [SerializeField] private UnityEvent onFocus;

        [Tooltip("Invoked when the Interact input is performed while this object is focused.")]
        [SerializeField] private UnityEvent onInteract;

        [Tooltip("Invoked when the raycast stops pointing at this object.")]
        [SerializeField] private UnityEvent onLoseFocus;

        /// <summary>
        /// The text shown for this object, as entered in <see cref="InteractData"/> (e.g. "Take Battery"), or
        /// the prefab's name if left empty. With LocalizationSystem, it is also the key of its translation.
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>Whether Interact must be held for <see cref="HoldDuration"/> instead of just pressed.</summary>
        public bool Holding => holding;

        /// <summary>Seconds Interact must be held when <see cref="Holding"/> is on.</summary>
        public float HoldDuration => holdDuration;

        internal void Focus() => onFocus?.Invoke();

        internal void Interact() => onInteract?.Invoke();

        internal void LoseFocus() => onLoseFocus?.Invoke();

        // A conditional [RequireComponent]: only add a BoxCollider when the object has no collider at all.
        private void Reset()
        {
            if (!TryGetComponent<Collider>(out _))
            {
                gameObject.AddComponent<BoxCollider>();
            }
        }
    }
}
