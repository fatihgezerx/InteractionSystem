using UnityEngine;

namespace InteractionSystem
{
    /// <summary>
    /// The computed texts of one <see cref="Interactable"/> for its Inspector: the Press / Hold pill, the prompt hints and the collider /
    /// layer checks. Every text is cached against the values it was made from, so the 200 ms polls allocate only when a value changes.
    /// One instance belongs to one Inspector item (it holds the component it was made for, never a static).
    /// </summary>
    internal sealed class InteractableStatus
    {
        private const string NoPromptMessage =
            "No prompt written yet. List this prefab in an Interact Data and press Compile; it writes the name and hold settings here.";

        private const string NoColliderMessage =
            "This object has no Collider, so detection can never hit it. Add one (a BoxCollider is what Compile adds).";

        private readonly Interactable target;
        private string promptName;
        private bool promptHolding;
        private float promptDuration = -1f;
        private string promptText = string.Empty;
        private float pillDuration = -1f;
        private string pillText = string.Empty;
        private string layerText;

        /// <summary>Makes the status of <paramref name="target"/>.</summary>
        /// <param name="target">The component to read; may be null (every text is then empty).</param>
        public InteractableStatus(Interactable target)
        {
            this.target = target;
        }

        /// <summary>The pill: "Press" (info) or "Hold n s" (special) while the prompt must be held.</summary>
        /// <returns>The text and the pill kind (<c>idle</c>, <c>info</c>, <c>special</c>).</returns>
        public (string text, string kind) PillState()
        {
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

        /// <summary>The hint that tells the written prompt and how it is used; null while no prompt is written.</summary>
        public string PromptText()
        {
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

        /// <summary>The warning shown while no prompt is written yet; null once there is one.</summary>
        public string NoPromptText()
        {
            return target != null && string.IsNullOrEmpty(target.DisplayName) ? NoPromptMessage : null;
        }

        /// <summary>The warning about a missing Collider; null when the object has one.</summary>
        public string NoColliderText()
        {
            return target != null && !target.TryGetComponent<Collider>(out _) ? NoColliderMessage : null;
        }

        /// <summary>The hint about an object that is not on the Interact layer; null when it is (or the layer does not exist).</summary>
        public string WrongLayerText()
        {
            if (target == null)
            {
                return null;
            }

            int layer = LayerMask.NameToLayer(InteractData.LayerName);
            if (layer < 0 || target.gameObject.layer == layer)
            {
                return null;
            }

            if (layerText == null)
            {
                layerText = "This object is not on the '" + InteractData.LayerName +
                            "' layer. Detection only sees the layers in the Interact Data's Raycast layers.";
            }

            return layerText;
        }
    }
}
