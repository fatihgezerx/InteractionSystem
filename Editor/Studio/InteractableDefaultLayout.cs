using System;
using Veneer;

namespace InteractionSystem
{
    /// <summary>
    /// The design of the <see cref="Interactable"/> Inspector that Interaction System ships: a Prompt card (what Compile wrote onto the
    /// object, read only) and an Events card (On Focus, On Interact, On Lose Focus), plus the system's own items (Press / Hold pill,
    /// prompt hints, collider and layer checks). With Veneer installed a project can copy it in the Inspector Designer and change the
    /// copy; this default is never saved as an asset.
    /// </summary>
    internal sealed class InteractableDefaultLayout : IVeneerDefaultLayoutProvider
    {
        /// <inheritdoc />
        public Type TargetType => typeof(Interactable);

        /// <inheritdoc />
        public VeneerLayout CreateLayout()
        {
            // One tab only, so no tab bar is drawn, as in the hand-written Inspector. The state pill (header badge) and the checks (footer,
            // drawn under the cards) are listed in the cards so the layout owns them; they are not drawn inside the card.
            VeneerLayout layout = new VeneerLayoutBuilder(typeof(Interactable), "Interactable", "Detected and used through an Interact Data")
                .Page("Interactable", "bolt")
                .Card("Prompt", "text")
                .Custom(InteractableItems.StateId)
                .Custom(InteractableItems.PromptId)
                .Card("Events", "bolt")
                .Field("onFocus", "On Focus")
                .Field("onInteract", "On Interact")
                .Field("onLoseFocus", "On Lose Focus")
                .Custom(InteractableItems.ChecksId)
                .Build();

            // The look of the system (accent, style sheet). UseThemeLogo stays off on purpose: the Inspector's builder already carries the
            // brand and shows its logo in the header.
            layout.ThemeBrand = InteractionBrand.Value.Name;
            return layout;
        }

        /// <inheritdoc />
        public void RegisterItems()
        {
            InteractableItems.Register();
        }
    }
}
