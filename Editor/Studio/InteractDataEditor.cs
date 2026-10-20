using Veneer;
using UnityEditor;

namespace InteractionSystem
{
    /// <summary>
    /// Studio-look Inspector for <see cref="InteractData"/>, drawn from a Veneer layout: the project's own copy made in the Inspector
    /// Designer when there is one, otherwise <see cref="InteractDataDefaultLayout"/> (tabs Detection, Origin and Interactables). The
    /// state pill, the checks and the Compile button are custom items (<see cref="InteractDataItems"/>). Only a selection of several
    /// assets is built here: a header, a hint and the Compile button.
    /// </summary>
    [CustomEditor(typeof(InteractData))]
    internal sealed class InteractDataEditor : VeneerLayoutDrivenInspector<InteractData>
    {
        /// <inheritdoc />
        protected override VeneerBrand Brand => InteractionBrand.Value;

        /// <inheritdoc />
        protected override bool BuildMultiSelection(VeneerInspectorBuilder b)
        {
            b.Header("Interact Data", "Detection and interactables");
            b.StatePill(new InteractDataStatus(Target).PillState);
            b.Hint("Several Interact Data assets are selected. Select one to edit it; Compile below works on all of them.");
            InteractDataItems.AddCompileButton(b);
            return true;
        }
    }
}
