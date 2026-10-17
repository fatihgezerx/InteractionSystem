using Veneer;
using UnityEditor;

namespace InteractionSystem
{
    /// <summary>
    /// Studio-look Inspector for <see cref="Interactable"/>, drawn from a Veneer layout: the project's own copy made in the Inspector
    /// Designer when there is one, otherwise <see cref="InteractableDefaultLayout"/> (a Prompt card and an Events card). The Press / Hold
    /// pill, the prompt hints and the collider / layer checks are custom items (<see cref="InteractableItems"/>). With several objects
    /// selected the layout is drawn for the first one, like for a single selection.
    /// </summary>
    [CustomEditor(typeof(Interactable))]
    [CanEditMultipleObjects]
    internal sealed class InteractableEditor : VeneerLayoutDrivenInspector<Interactable>
    {
        /// <inheritdoc />
        protected override VeneerBrand Brand => InteractionBrand.Value;
    }
}
