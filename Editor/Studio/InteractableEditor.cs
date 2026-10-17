using Veneer;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace InteractionSystem
{
    /// <summary>
    /// Studio-look Inspector for <see cref="Interactable"/>: a header with a Press / Hold pill, the prompt that
    /// <see cref="InteractData"/> wrote onto it (read only, the values are hidden fields), the three UnityEvents and
    /// hints for a missing collider or a layer other than <c>Interact</c>.
    /// </summary>
    [CustomEditor(typeof(Interactable))]
    [CanEditMultipleObjects]
    internal sealed class InteractableEditor : VeneerInspector<Interactable>
    {
        private string promptName;
        private bool promptHolding;
        private float promptDuration = -1f;
        private string promptText = string.Empty;
        private float pillDuration = -1f;
        private string pillText = string.Empty;

        /// <inheritdoc />
        protected override VeneerBrand Brand => InteractionBrand.Value;

        /// <inheritdoc />
        protected override void Build(VeneerInspectorBuilder b)
        {
            b.Header("Interactable", "Detected and used through an Interact Data");
            b.StatePill(PillState);

            b.Card("Prompt", "text", () =>
            {
                b.ConditionalHint(PromptText, "info", true);
                b.ConditionalHint(
                    () => string.IsNullOrEmpty(Target.DisplayName)
                        ? "No prompt written yet. List this prefab in an Interact Data and press Compile; it writes the name and hold settings here."
                        : null,
                    "warn",
                    true);
            });

            b.Card("Events", "bolt", () =>
            {
                b.Field("onFocus", "On Focus");
                b.Field("onInteract", "On Interact");
                b.Field("onLoseFocus", "On Lose Focus");
            });

            b.ConditionalHint(
                () => Target != null && !Target.TryGetComponent<Collider>(out _)
                    ? "This object has no Collider, so detection can never hit it. Add one (a BoxCollider is what Compile adds)."
                    : null,
                "warn",
                true);
            b.ConditionalHint(
                () =>
                {
                    int layer = LayerMask.NameToLayer(InteractData.LayerName);
                    return Target != null && layer >= 0 && Target.gameObject.layer != layer
                        ? "This object is not on the '" + InteractData.LayerName + "' layer. Detection only sees the layers in the Interact Data's Raycast layers."
                        : null;
                },
                "info",
                true);
        }

        private (string text, string kind) PillState()
        {
            Interactable target = Target;
            if (target == null)
            {
                return (string.Empty, "idle");
            }

            if (!target.Holding)
            {
                return ("Press", "info");
            }

            if (!Mathf.Approximately(pillDuration, target.HoldDuration))
            {
                pillDuration = target.HoldDuration;
                pillText = "Hold " + pillDuration.ToString("0.##") + " s";
            }

            return (pillText, "special");
        }

        private string PromptText()
        {
            Interactable target = Target;
            if (target == null || string.IsNullOrEmpty(target.DisplayName))
            {
                return null;
            }

            if (promptName != target.DisplayName || promptHolding != target.Holding || !Mathf.Approximately(promptDuration, target.HoldDuration))
            {
                promptName = target.DisplayName;
                promptHolding = target.Holding;
                promptDuration = target.HoldDuration;
                promptText = "Prompt \"" + promptName + "\", " +
                             (promptHolding ? "hold Interact for " + promptDuration.ToString("0.##") + " s." : "press Interact.") +
                             " Set in the Interact Data (Compile writes it here).";
            }

            return promptText;
        }
    }
}
