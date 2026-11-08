using System;
using IL2CppSharp;

using AstralParty.Modding.SDK.Native.Interop;
using AstralParty.Modding.SDK.Native.UI;

namespace AstralParty.Modding.SDK.Native.Interop {
    /// <summary>
    /// Shared candidate rules for the native HeroPanel supplements and
    /// account statistics. This is NOT a check of installed Addressables.
    /// It verifies only the same client item validity and config data needed
    /// by HeroCardLogic and CharacterExpression.
    /// </summary>
    public static class NativeHeroCosmeticEligibility {
        private static bool IsValidItem(IntPtr item) {
            if (item == IntPtr.Zero) return false;
            IntPtr method = FairyBridge.TryMethod("ItemInfoConfigure", "IsVailItem", 0);
            return method != IntPtr.Zero &&
                Il2CppReflection.InvokeMethodGetBool(method, item);
        }

        public static bool CanRenderSkin(IntPtr variant, IntPtr itemDict) {
            if (variant == IntPtr.Zero || itemDict == IntPtr.Zero) return false;
            int id = FairyBridge.GetInt(variant, "SkinStandingPaintingConfigureItem", "ItemID");
            if (id <= 0) return false;
            IntPtr item = NativeCatalogLookup.GetDictionaryItem(itemDict, id);
            if (!IsValidItem(item)) return false;
            int nameId = FairyBridge.GetInt(item, "ItemInfoConfigure", "NameID");
            if (!NativeCatalogLookup.HasLocal("Item", nameId)) return false;
            string image = FairyBridge.GetString(variant,
                "SkinStandingPaintingConfigureItem", "CharacterReady");
            string safeImage = FairyBridge.GetString(variant,
                "SkinStandingPaintingConfigureItem", "SfwCharacterReady");
            return !string.IsNullOrWhiteSpace(image) ||
                !string.IsNullOrWhiteSpace(safeImage);
        }

        public static bool CanRenderExpression(IntPtr expression, IntPtr itemDict) {
            if (expression == IntPtr.Zero || itemDict == IntPtr.Zero) return false;
            int id = FairyBridge.GetInt(expression,
                "CharacterExpressionPackConfigureItem", "ItemID");
            if (id <= 0) return false;
            IntPtr item = NativeCatalogLookup.GetDictionaryItem(itemDict, id);
            if (!IsValidItem(item)) return false;
            string video = FairyBridge.GetString(expression,
                "CharacterExpressionPackConfigureItem", "VideoKey");
            string icon = FairyBridge.GetString(item, "ItemInfoConfigure", "ShowIcon");
            return !string.IsNullOrWhiteSpace(video) ||
                !string.IsNullOrWhiteSpace(icon);
        }

        public static bool IsOwned(IntPtr bag, int itemId) {
            if (bag == IntPtr.Zero || itemId <= 0) return false;
            return Il2CppReflection.InvokeGet<bool>(
                FairyBridge.Method("GameLogic.BagLogic", "ExistItem", 1), bag,
                [Il2CppReflection.BoxValue(FairyBridge.Class("System.Int32"), itemId)]);
        }

        public static bool IsOwnedSkin(IntPtr bag, IntPtr variant) {
            if (variant == IntPtr.Zero) return false;
            if (FairyBridge.GetBool(variant, "SkinStandingPaintingConfigureItem", "IsDefault")) return true;
            return IsOwned(bag, FairyBridge.GetInt(variant,
                "SkinStandingPaintingConfigureItem", "ItemID"));
        }
    }
}
