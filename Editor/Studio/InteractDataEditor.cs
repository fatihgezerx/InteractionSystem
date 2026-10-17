using Veneer;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace InteractionSystem
{
    /// <summary>
    /// Studio-look Inspector for <see cref="InteractData"/>. Tabs: Interactables (the prefab list with a state pill per row, the
    /// outline settings), Detection (shape, reach, check interval, layers) and Origin (tag, child, offset). A primary Compile button
    /// under the tabs runs <see cref="InteractCompiler.Compile"/> for the selected asset(s).
    /// </summary>
    [CustomEditor(typeof(InteractData))]
    internal sealed class InteractDataEditor : VeneerInspector<InteractData>
    {
        private const string NameInfo =
            "Each object's Name is shown exactly as written, so write the whole prompt (e.g. \"Take Battery\"). " +
            "With LocalizationSystem, the whole Name is translated as one sentence (e.g. \"Bataryayı al\"). " +
            "Leave it empty to show the prefab's name.";

        private const string CompileTooltip =
            "Gives every listed prefab an Interactable (only if missing), a BoxCollider (only if it has no collider), the Interact layer " +
            "(created if missing), its name and hold settings. Prefabs that are already up to date are skipped.";

        // Cached texts, so the 200 ms polls allocate only when a count changes.
        private int pillKey = -1;
        private string pillText = string.Empty;
        private int problemsKey = -1;
        private string problemsText;
        private int duplicatesKey = -1;
        private string duplicatesText;
        private int needsKey = -1;
        private string needsText;

        /// <inheritdoc />
        protected override VeneerBrand Brand => InteractionBrand.Value;

        /// <inheritdoc />
        protected override void Build(VeneerInspectorBuilder b)
        {
            b.Header(Target.name, "Interact Data: detection and interactables");
            b.StatePill(PillState);

            if (TargetCount > 1)
            {
                b.Hint("Several Interact Data assets are selected. Select one to edit it; Compile below works on all of them.");
                AddCompileButton(b);
                return;
            }

            VisualElement[] tabs = b.Tabs(("Interactables", "list"), ("Detection", "target"), ("Origin", "anchor"));

            using (b.Into(tabs[0]))
            {
                b.Card("Interactables", "list", () =>
                {
                    b.Hint(NameInfo);
                    b.Add(new InteractEntryListView(b.SerializedObject.FindProperty("interactables"), Brand));
                    b.ConditionalHint(ProblemsText, "warn", true);
                    b.ConditionalHint(DuplicatesText, "warn", true);
                    b.ConditionalHint(NeedsCompileText, "info", true);
                });

                b.Card("Outline", "overlay", () =>
                {
                    b.Chip("generalSettings.showOutline", "Show outline", "eye");
#if !HAS_OUTLINE_SYSTEM
                    b.ConditionalHint(
                        () => Target.Settings.ShowOutline ? "Outline System isn't in the project - Compile will skip it until it is." : null,
                        "warn");
#endif
                    VisualElement color = b.Color("generalSettings.outlineColor", "Color");
                    VisualElement width = b.Slider("generalSettings.outlineWidth", "Width", "Thin", "Thick");
                    b.VisibleWhen(color, "generalSettings.showOutline", true);
                    b.VisibleWhen(width, "generalSettings.showOutline", true);
                });
            }

            using (b.Into(tabs[1]))
            {
                b.Card("Shape", "target", () =>
                {
                    b.Enum(
                        "generalSettings.raycastType",
                        new[] { "arrow-right", "sphere" },
                        new[]
                        {
                            "Line: a ray Reach long. The first thing it hits decides, so walls block it. Suited to first-person games.",
                            "Sphere: a sphere of Reach around the origin; the closest interactable inside wins. Suited to third-person games.",
                        },
                        "Shape");
                    b.ConditionalHint(
                        () => Target.Settings.RaycastType == RaycastType.Line
                            ? "Line never sees through anything: a wall in front of an interactable (on a layer that is not in Raycast layers) blocks it."
                            : "Sphere detects every interactable within Reach, walls or not, and focuses the closest one.",
                        "info");
                    b.Slider("generalSettings.radius", "Reach", "Short", "Long", float.NaN, float.NaN, "m");
                    b.Slider("generalSettings.checkInterval", "Check interval", "Fast", "Relaxed", float.NaN, float.NaN, "s");
                });

                b.Card("Layers", "layers", () =>
                {
                    b.Field("generalSettings.raycastLayers", "Raycast layers");
                    b.ConditionalHint(
                        () => Target.Settings.RaycastLayers.value == 0 ? "No layer is selected: nothing will be detected." : null,
                        "warn");
                });
            }

            using (b.Into(tabs[2]))
            {
                b.Card("Origin object", "anchor", () =>
                {
                    b.Add(CreateTagRow(b.SerializedObject.FindProperty("generalSettings.originTag")));
                    b.ConditionalHint(
                        () => string.IsNullOrEmpty(Target.Settings.OriginTag) ? "Origin tag is empty: detection has no object to start from." : null,
                        "warn");
                    b.Chip("generalSettings.inChild", "Start from a child", "link");
                    VisualElement child = b.Number("generalSettings.childCount", "Child index");
                    b.VisibleWhen(child, "generalSettings.inChild", true);
                });

                b.Card("Offset", "move", () =>
                {
                    b.Vector3("generalSettings.localPosition", "Local position");
                    b.Vector3("generalSettings.localRotation", "Local rotation");
                });
            }

            AddCompileButton(b);
        }

        private void AddCompileButton(VeneerInspectorBuilder b)
        {
            b.Button("Compile", "wrench", CompileTargets, CompileTooltip, true);
        }

        private void CompileTargets()
        {
            foreach (Object asset in targets)
            {
                if (asset is InteractData data)
                {
                    InteractCompiler.Compile(data);
                }
            }
        }

        // The origin tag is a tag dropdown (TagField), kept from the old Inspector; wrapped in a kit row for the label and the prefab mark.
        private static VisualElement CreateTagRow(SerializedProperty property)
        {
            property = property.Copy();
            var field = new TagField { tooltip = property.tooltip };
            field.BindProperty(property);
            VisualElement row = VeneerStyle.FieldRow("Origin tag", field, property.tooltip, out Label label);
            VeneerBinding.Hook(row, property, label, row, null);
            return row;
        }

        private (string text, string kind) PillState()
        {
            InteractData data = Target;
            if (data == null)
            {
                return (string.Empty, "idle");
            }

            int total = data.Interactables.Count;
            if (total == 0)
            {
                return ("Empty", "idle");
            }

            InteractCompiler.Analyze(data, out int needs, out int problems, out int duplicates, out int upToDate);
            int key = (needs * 4096) + (problems * 64) + duplicates + (upToDate << 20);
            if (key != pillKey)
            {
                pillKey = key;
                if (problems + duplicates > 0)
                {
                    pillText = problems + duplicates + (problems + duplicates == 1 ? " issue" : " issues");
                }
                else if (needs > 0)
                {
                    pillText = needs + " to compile";
                }
                else
                {
                    pillText = "All compiled";
                }
            }

            string kind = problems + duplicates > 0 || needs > 0 ? "warn" : "ok";
            return (pillText, kind);
        }

        private string ProblemsText()
        {
            InteractData data = Target;
            if (data == null)
            {
                return null;
            }

            InteractCompiler.Analyze(data, out _, out int problems, out _, out _);
            if (problems == 0)
            {
                return null;
            }

            if (problems != problemsKey)
            {
                problemsKey = problems;
                problemsText = problems + (problems == 1 ? " entry has" : " entries have") +
                               " no prefab or an object that is not a prefab asset; Compile skips them.";
            }

            return problemsText;
        }

        private string DuplicatesText()
        {
            InteractData data = Target;
            if (data == null)
            {
                return null;
            }

            InteractCompiler.Analyze(data, out _, out _, out int duplicates, out _);
            if (duplicates == 0)
            {
                return null;
            }

            if (duplicates != duplicatesKey)
            {
                duplicatesKey = duplicates;
                duplicatesText = duplicates + (duplicates == 1 ? " entry lists" : " entries list") +
                                 " a prefab that is already listed above; the last one decides what Compile writes.";
            }

            return duplicatesText;
        }

        private string NeedsCompileText()
        {
            InteractData data = Target;
            if (data == null)
            {
                return null;
            }

            InteractCompiler.Analyze(data, out int needs, out _, out _, out _);
            if (needs == 0)
            {
                return null;
            }

            if (needs != needsKey)
            {
                needsKey = needs;
                needsText = needs + (needs == 1 ? " prefab differs" : " prefabs differ") + " from this list. Press Compile to update them.";
            }

            return needsText;
        }
    }
}
