using System;
using Veneer;
using UnityEditor;
using UnityEngine.UIElements;

namespace InteractionSystem
{
    /// <summary>
    /// The pieces of the <see cref="InteractData"/> Inspector that are not fields, registered with Veneer so the default layout (and a
    /// user's copy of it) can place them: the state pill, the Interactable checks, the missing-Outline-System hint and the Compile
    /// button, plus the widget of the <c>interactables</c> list. They can be moved or hidden in the Inspector Designer, not changed.
    /// </summary>
    internal static class InteractDataItems
    {
        /// <summary>Id of the header state pill (header badge).</summary>
        public const string StateId = "interact.state";

        /// <summary>Id of the three computed hints under the list (card item).</summary>
        public const string ChecksId = "interact.checks";

        /// <summary>Id of the "Outline System is missing" hint (card item).</summary>
        public const string OutlineMissingId = "interact.outline-missing";

        /// <summary>Id of the Compile button (footer).</summary>
        public const string CompileId = "interact.compile";

        /// <summary>The tooltip of the Compile button.</summary>
        public const string CompileTooltip =
            "Gives every listed prefab an Interactable (only if missing), a BoxCollider (only if it has no collider), the Interact layer " +
            "(created if missing), its name and hold settings. Prefabs that are already up to date are skipped.";

        /// <summary>Registers the custom items and the list widget of <see cref="InteractData"/>. Safe to call again (items are replaced).</summary>
        public static void Register()
        {
            Type type = typeof(InteractData);

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = StateId,
                Label = "State",
                Icon = "check-circle",
                Description = "A pill in the header: Empty, n issues, n to compile or All compiled.",
                Slot = VeneerCustomItemSlot.HeaderBadge,
                Build = BuildState,
            });

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = ChecksId,
                Label = "Interactable checks",
                Icon = "warning",
                Description = "Hints under the list: entries Compile skips, duplicate prefabs and prefabs that differ from the list.",
                Slot = VeneerCustomItemSlot.Item,
                Build = BuildChecks,
            });

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = OutlineMissingId,
                Label = "Outline System check",
                Icon = "warning",
                Description = "A warning while Show outline is on and the Outline System is not in the project.",
                Slot = VeneerCustomItemSlot.Item,
                Build = BuildOutlineMissing,
            });

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = CompileId,
                Label = "Compile button",
                Icon = "wrench",
                Description = "Runs Compile for the selected Interact Data. Drawn at the bottom, whatever tab is open.",
                Slot = VeneerCustomItemSlot.Footer,
                Build = BuildCompile,
            });

            VeneerCustomItems.RegisterWidget(type, "interactables", (context, property) => new InteractEntryListView(property, context.Brand));
        }

        /// <summary>Adds the primary Compile button to the builder's current container; it compiles every selected Interact Data.</summary>
        /// <param name="b">The builder of the Inspector being built.</param>
        public static void AddCompileButton(VeneerInspectorBuilder b)
        {
            SerializedObject serializedObject = b.SerializedObject;
            b.Button("Compile", "wrench", () => CompileTargets(serializedObject), CompileTooltip, true);
        }

        private static void CompileTargets(SerializedObject serializedObject)
        {
            foreach (UnityEngine.Object asset in serializedObject.targetObjects)
            {
                if (asset is InteractData data)
                {
                    InteractCompiler.Compile(data);
                }
            }
        }

        private static VisualElement BuildState(VeneerCustomItemContext context)
        {
            var status = new InteractDataStatus(context.SerializedObject.targetObject as InteractData);
            Label pill = VeneerStyle.Pill(string.Empty, "idle");
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

        private static VisualElement BuildChecks(VeneerCustomItemContext context)
        {
            var status = new InteractDataStatus(context.SerializedObject.targetObject as InteractData);
            context.Builder.ConditionalHint(status.ProblemsText, "warn", true);
            context.Builder.ConditionalHint(status.DuplicatesText, "warn", true);
            context.Builder.ConditionalHint(status.NeedsCompileText, "info", true);
            return null;
        }

        private static VisualElement BuildOutlineMissing(VeneerCustomItemContext context)
        {
#if !HAS_OUTLINE_SYSTEM
            var data = context.SerializedObject.targetObject as InteractData;
            context.Builder.ConditionalHint(
                () => data != null && data.Settings.ShowOutline
                    ? "Outline System isn't in the project - Compile will skip it until it is."
                    : null,
                "warn");
#endif
            return null;
        }

        private static VisualElement BuildCompile(VeneerCustomItemContext context)
        {
            AddCompileButton(context.Builder);
            return null;
        }
    }
}
