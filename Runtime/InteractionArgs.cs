namespace InteractionSystem
{
    /// <summary>
    /// Event data for <c>EventTypes.OnFocus</c>, <c>OnLoseFocus</c>, <c>OnInteract</c> and
    /// <c>OnInteracting</c>: the <see cref="Interactable"/> the event is about - its
    /// <see cref="Interactable.DisplayName"/>, <see cref="Interactable.Holding"/> and
    /// <see cref="Interactable.HoldDuration"/> included.
    /// <code>
    /// EventManager.RegisterEvent&lt;InteractionArgs&gt;(EventTypes.OnInteract, OnInteract);
    /// </code>
    /// </summary>
    /// <remarks>
    /// <c>OnInteracting</c> fires every frame, so it reuses a single instance to avoid allocating;
    /// never keep a reference to the args past the callback.
    /// </remarks>
    public sealed class InteractionArgs
    {
        public Interactable Value;

        /// <summary>Seconds Interact has been held so far. Set only by <c>OnInteracting</c>; 0 otherwise.</summary>
        public float Elapsed;

        public InteractionArgs(Interactable value, float elapsed = 0f)
        {
            Value = value;
            Elapsed = elapsed;
        }
    }
}
