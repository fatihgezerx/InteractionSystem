using Veneer;
using UnityEngine;

namespace InteractionSystem
{
    /// <summary>The identity of Interaction System in the shared editor look: name, amber accent, logo and banner.</summary>
    internal static class InteractionBrand
    {
        private const string BrandFolder = "Assets/Scripts/InteractionSystem/Editor/Brand/";

        /// <summary>The cached brand; its pictures load lazily from <c>Editor/Brand</c>.</summary>
        public static readonly VeneerBrand Value = VeneerBrand.FromAssets(
            name: "Interaction System",
            subtitle: "Detect, focus and interact",
            accent: new Color32(0xF5, 0xA5, 0x24, 255),
            logoPath: BrandFolder + "logo.png",
            bannerPath: BrandFolder + "banner.png",
            iconFolder: null,
            styleSheetPath: BrandFolder + "InteractionBrand.uss",
            accentClass: "vn-accent--amber");
    }
}
