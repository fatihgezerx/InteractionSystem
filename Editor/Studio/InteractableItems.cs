using System;
using Veneer;
using UnityEngine.UIElements;

namespace InteractionSystem
{
    /// <summary>
    /// The pieces of the <see cref="Interactable"/> Inspector that are not fields, registered with Veneer so the default layout (and a
    /// user's copy of it) can place them: the Press / Hold pill, the prompt hints and the collider / layer checks. They can be moved or
    /// hidden in the Inspector Designer, not changed. Each built item makes its own <see cref="InteractableStatus"/>, so nothing is shared.
    /// </summary>
    internal static class InteractableItems
    {
        /// <summary>Id of the header pill: Press, or Hold n s (header badge).</summary>
        public const string StateId = "interactable.state";

        /// <summary>Id of the two prompt hints: the written prompt, or "no prompt written yet" (card item).</summary>
        public const string PromptId = "interactable.prompt";

        /// <summary>Id of the missing-Collider and wrong-layer hints (footer, drawn under the cards).</summary>
        public const string ChecksId = "interactable.checks";

        /// <summary>Registers the custom items of <see cref="Interactable"/>. Safe to call again (items are replaced).</summary>
        public static void Register()
        {
            Type type = typeof(Interactable);

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = StateId,
                Label = "Mode pill",
                Icon = "check-circle",
                Description = "A pill in the header: Press, or Hold n s when the prompt must be held.",
                Slot = VeneerCustomItemSlot.HeaderBadge,
                Build = BuildState,
            });

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = PromptId,
                Label = "Prompt hints",
                Icon = "text",
                Description = "The prompt Compile wrote onto this object (and how to use it), or a warning while none is written yet.",
                Slot = VeneerCustomItemSlot.Item,
                Build = BuildPrompt,
            });

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = ChecksId,
                Label = "Collider and layer checks",
                Icon = "warning",
                Description = "Hints under the cards: the object has no Collider, or is not on the Interact layer. Drawn at the bottom, whatever tab is open.",
                Slot = VeneerCustomItemSlot.Footer,
                Build = BuildChecks,
            });
        }

        private static InteractableStatus StatusOf(VeneerCustomItemContext context)
        {
            return new InteractableStatus(context.SerializedObject.targetObject as Interactable);
        }

        private static VisualElement BuildState(VeneerCustomItemContext context)
        {
            InteractableStatus status = StatusOf(context);
            Label pill = VeneerStyle.Pill(string.Empty, "info");
            Action refresh = () =>
            {
                (string text, string kind) state = status.PillState();
                VeneerStyle.SetPill(pill, state.kind, state.text);
            };

            // The first run of Poll is on the first tick: show the right state at once.
            refresh();
            context.Poll(refresh);
            return pill;
        }

        private static VisualElement BuildPrompt(VeneerCustomItemContext context)
        {
            InteractableStatus status = StatusOf(context);
            context.Builder.ConditionalHint(status.PromptText, "info", true);
            context.Builder.ConditionalHint(status.NoPromptText, "warn", true);
            return null;
        }

        private static VisualElement BuildChecks(VeneerCustomItemContext context)
        {
            InteractableStatus status = StatusOf(context);
            context.Builder.ConditionalHint(status.NoColliderText, "warn", true);
            context.Builder.ConditionalHint(status.WrongLayerText, "info", true);
            return null;
        }
    }
}
