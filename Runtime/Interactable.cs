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
        [HideInInspector] [SerializeField] internal string header;
        [HideInInspector] [SerializeField] internal string displayName;
        [HideInInspector] [SerializeField] internal bool holding;
        [HideInInspector] [SerializeField] internal float holdDuration = 1f;

        [Tooltip("Invoked when the raycast starts pointing at this object.")]
        [SerializeField] private UnityEvent onFocus;

        [Tooltip("Invoked when the Interact input is performed while this object is focused.")]
        [SerializeField] private UnityEvent onInteract;

        [Tooltip("Invoked when the raycast stops pointing at this object.")]
        [SerializeField] private UnityEvent onLoseFocus;

        /// <summary>The header of this object's group in <see cref="InteractData"/>, e.g. "Open". May be empty.</summary>
        public string Header => header;

        /// <summary>The object's name as entered in <see cref="InteractData"/>, e.g. "Door".</summary>
        public string DisplayName => displayName;

        /// <summary>
        /// <see cref="Header"/> in front of <see cref="DisplayName"/>, ready for UI - e.g. "Open Door".
        /// Just the name when the group has no header.
        /// </summary>
        public string FullName => string.IsNullOrWhiteSpace(header) ? displayName : $"{header.Trim()} {displayName}";

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
