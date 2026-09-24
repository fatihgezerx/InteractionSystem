namespace InteractionSystem
{
    // Published through EventSystem's EventManager, e.g.:
    //     EventManager.Register<FocusEvent>(OnFocus);
    // All of them are readonly structs, so raising them never allocates.

    /// <summary>Detection started pointing at <see cref="Target"/>.</summary>
    public readonly struct FocusEvent
    {
        public readonly Interactable Target;

        public FocusEvent(Interactable target) => Target = target;
    }

    /// <summary>Detection stopped pointing at <see cref="Target"/>.</summary>
    public readonly struct LoseFocusEvent
    {
        public readonly Interactable Target;

        public LoseFocusEvent(Interactable target) => Target = target;
    }

    /// <summary>Interact was pressed (or held for its Duration) while <see cref="Target"/> was focused.</summary>
    public readonly struct InteractEvent
    {
        public readonly Interactable Target;

        public InteractEvent(Interactable target) => Target = target;
    }

    /// <summary>
    /// A Holding <see cref="Target"/> is being held: <see cref="Elapsed"/> seconds so far. Raised with 0
    /// when the hold starts, every frame while it lasts, and with 0 again when it ends (released, focus
    /// lost, or completed right after <see cref="InteractEvent"/>).
    /// </summary>
    public readonly struct InteractingEvent
    {
        public readonly Interactable Target;
        public readonly float Elapsed;

        public InteractingEvent(Interactable target, float elapsed)
        {
            Target = target;
            Elapsed = elapsed;
        }
    }
}
