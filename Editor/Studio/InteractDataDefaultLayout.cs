using System;
using Veneer;

namespace InteractionSystem
{
    /// <summary>
    /// The design of the <see cref="InteractData"/> Inspector that Interaction System ships: three tabs (Detection, Origin,
    /// Interactables), their cards and fields, notes that show or hide with the fields they explain, and the system's own items (state pill,
    /// Interactable checks, Outline System check, Compile button). With Veneer installed a project can copy it in the Inspector Designer
    /// and change the copy; this default is never saved as an asset.
    /// </summary>
    internal sealed class InteractDataDefaultLayout : IVeneerDefaultLayoutProvider
    {
        private const string NameInfo =
            "Each object's Name is shown exactly as written, so write the whole prompt (e.g. \"Take Battery\"). " +
            "With LocalizationSystem, the whole Name is translated as one sentence (e.g. \"Bataryayı al\"). " +
            "Leave it empty to show the prefab's name.";

        private const string ShapeTooltip =
            "Line: a ray Reach long. The first thing it hits decides, so walls block it. Suited to first-person games.\n" +
            "Sphere: a sphere of Reach around the origin; the closest interactable inside wins. Suited to third-person games.";

        private const string GeneralSettings = "generalSettings.";

        /// <inheritdoc />
        public Type TargetType => typeof(InteractData);

        /// <inheritdoc />
        public VeneerLayout CreateLayout()
        {
            VeneerLayout layout = new VeneerLayoutBuilder(typeof(InteractData), "Interact Data", "Detection and interactables")
                .Page("Detection", "target")
                .Card("Layers", "layers")
                .Field(GeneralSettings + "raycastLayers", "Raycast layers")
                .Note("No layer is selected: nothing will be detected.", VeneerNoteKind.Warning)
                .ShowIf(GeneralSettings + "raycastLayers", VeneerCondition.IsEmpty)
                .Card("Shape", "target")
                .Enum(GeneralSettings + "raycastType", "Shape", "arrow-right,sphere")
                .Note("Line never sees through anything: a wall in front of an interactable (on a layer that is not in Raycast layers) blocks it.")
                .ShowIf(GeneralSettings + "raycastType", VeneerCondition.Equals, "Line")
                .Note("Sphere detects every interactable within Reach, walls or not, and focuses the closest one.")
                .ShowIf(GeneralSettings + "raycastType", VeneerCondition.Equals, "Sphere")
                .Slider(GeneralSettings + "radius", "Reach", "Short", "Long", "m")
                .Slider(GeneralSettings + "checkInterval", "Check interval", "Fast", "Relaxed", "s")
                .Card("Outline", "overlay")
                .Field(GeneralSettings + "showOutline", "Show outline", widget: VeneerWidget.Toggle)
                .Custom(InteractDataItems.OutlineMissingId)
                .Field(GeneralSettings + "outlineColor", "Color", widget: VeneerWidget.Color)
                .ShowIf(GeneralSettings + "showOutline", VeneerCondition.IsTrue)
                .Slider(GeneralSettings + "outlineWidth", "Width", "Thin", "Thick")
                .ShowIf(GeneralSettings + "showOutline", VeneerCondition.IsTrue)
                .Page("Origin", "anchor")
                .Card("Origin object", "anchor")
                .Field(GeneralSettings + "originTag", "Origin tag", widget: VeneerWidget.Tag)
                .Note("Origin tag is empty: detection has no object to start from.", VeneerNoteKind.Warning)
                .ShowIf(GeneralSettings + "originTag", VeneerCondition.IsEmpty)
                .Field(GeneralSettings + "inChild", "Start from a child", widget: VeneerWidget.Toggle)
                .Field(GeneralSettings + "childCount", "Child index", widget: VeneerWidget.Number)
                .ShowIf(GeneralSettings + "inChild", VeneerCondition.IsTrue)
                .Card("Offset", "move")
                .Field(GeneralSettings + "localPosition", "Local position", widget: VeneerWidget.Vector)
                .Field(GeneralSettings + "localRotation", "Local rotation", widget: VeneerWidget.Vector)
                .Page("Interactables", "list")
                .Card("Interactables", "list")
                .Note(NameInfo)
                .Field("interactables")
                .Custom(InteractDataItems.ChecksId)
                .Custom(InteractDataItems.StateId)
                .Custom(InteractDataItems.CompileId)
                .Build();

            // The segmented Shape buttons have icons only, so the explanation of both shapes goes on the row.
            VeneerLayoutEntry shape = layout.FindEntry(GeneralSettings + "raycastType", out _, out _);
            if (shape != null)
            {
                shape.Tooltip = ShapeTooltip;
            }

            // The look of the system (accent, style sheet). UseThemeLogo stays off on purpose: the Inspector's builder already carries the
            // brand and shows its logo in the header, and the renderer would put a second logo next to it.
            layout.ThemeBrand = InteractionBrand.Value.Name;
            return layout;
        }

        /// <inheritdoc />
        public void RegisterItems()
        {
            InteractDataItems.Register();
        }
    }
}
