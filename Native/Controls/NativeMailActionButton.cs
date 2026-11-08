#nullable enable
using System;
using IL2CppSharp;
using UnityEngine;


namespace AstralParty.Modding.SDK.Native {
    /// <summary>Clone the actual Mail/btn_All resource; never substitute a settings tab or draw a backing.</summary>
    public static class NativeMailActionButton {
        private static readonly NativeUiPackageLoader Package = new("Mail");

        public static bool TryCreate(IntPtr parent, string title, out IntPtr button, out IntPtr text) {
            button = text = IntPtr.Zero;
            if (parent == IntPtr.Zero || !Package.Ready()) return false;
            IntPtr template = Il2CppReflection.InvokeMethod(FairyBridge.Method("UI.UIMailPanel", "CreateInstance", 0), IntPtr.Zero, Array.Empty<IntPtr>());
            if (template == IntPtr.Zero) {
                return false;
            }
            using var templateLease = new NativeObjectLease(template);
            try {
                IntPtr source = FairyBridge.FieldValue(template, "UI.UIMailPanel", "btn_All");
                if (source == IntPtr.Zero) return false;
                string url = FairyBridge.GetString(source, "FairyGUI.GObject", "resourceURL");
                if (string.IsNullOrEmpty(url)) return false;
                button = Il2CppReflection.InvokeMethodWithString(FairyBridge.Method("FairyGUI.UIPackage", "CreateObjectFromURL", 1), IntPtr.Zero, url);
                using var constructionLease = new NativeObjectLease(button);
                if (button == IntPtr.Zero) return false;
                text = NativeReferenceFields.Call(button, "GetTextField");
                if (text == IntPtr.Zero) throw new InvalidOperationException("Native mail button has no text field");
                // Remove size gears only on this fresh clone. Native color and
                // pressed-state artwork remain; controller changes cannot shrink
                // the fitted backing to the prefab's original width again.
                DetachSizeGears(button, 0);
                NativeToggleControl.EnableSelectionOnClick(button);
                FairyBridge.SetBool(button, "FairyGUI.GButton", "selected", false);
                FairyBridge.SetString(button, "FairyGUI.GButton", "title", NativeSdkServices.Translate(title));
                FairyBridge.SetString(button, "FairyGUI.GButton", "selectedTitle", "");
                FairyBridge.SetBool(button, "FairyGUI.GObject", "enabled", true);
                FairyBridge.SetBool(button, "FairyGUI.GObject", "visible", true);
                NativeToggleControl.EnsureFullRectHitArea(button);
                StableNativeAction.Prepare(button, text);
                FairyBridge.AddChild(parent, button);
                return true;
            } catch {
                if (button != IntPtr.Zero) NativeReferenceFields.Call(button, "Dispose");
                button = text = IntPtr.Zero;
                throw;
            } finally {
                if (template != IntPtr.Zero) NativeReferenceFields.Call(template, "Dispose");
            }
        }

        public static void DetachSizeGears(IntPtr obj, int depth) {
            if (obj == IntPtr.Zero || depth > 5) return;
            IntPtr gear = NativeReferenceFields.Call(obj, "GetGear", NativePreviewList.Box(2));
            if (gear != IntPtr.Zero) {
                NativeReferenceFields.Call(gear, "Dispose");
                NativeReferenceFields.Call(gear, "set_controller", IntPtr.Zero);
            }
            if (!NativeGame.IsInstanceOf(obj, "FairyGUI.GComponent")) return;
            for (int i = 0, count = Math.Min(64, FairyBridge.ChildCount(obj)); i < count; i++)
                DetachSizeGears(FairyBridge.GetChildAt(obj, i), depth + 1);
        }

        public static float Fit(IntPtr button, IntPtr text, float availableWidth, float targetHeight = 0f) {
            int size = NativeSdkServices.ActionButtonFontSize;
            NativeFairyText.ApplyStyle(text, size, 0xFFFFFF);
            StableNativeAction.SetCaption(button, FairyBridge.GetString(button, "FairyGUI.GButton", "title"));
            // Reset the previous shrink bounds before measuring a changed caption.
            FairyBridge.SetSize(text, Math.Max(160f, availableWidth), targetHeight > 0f ? targetHeight : 64f);
            // Measure the real localized caption with its own native font.
            FairyBridge.SetInt(text, "FairyGUI.GTextField", "autoSize", 1);
            float desired = (float)Math.Ceiling(FairyBridge.GetFloat(text, "FairyGUI.GTextField", "textWidth")) + 64f;
            float width = Math.Min(Math.Max(160f, availableWidth), Math.Max(180f, desired));
            float height = targetHeight > 0f ? targetHeight : Math.Max(56f, FairyBridge.GetFloat(button, "FairyGUI.GObject", "height"));
            FairyBridge.SetInt(text, "FairyGUI.GTextField", "autoSize", 0);
            NativeActionButton.StretchNativeArtwork(button, width, height);
            FairyBridge.SetXY(text, 24f, 0f);
            FairyBridge.SetSize(text, Math.Max(1f, width - 48f), height);
            FairyBridge.CenterText(text);
            // Keep one font size. Wrap when the viewport cannot fit the caption.
            FairyBridge.SetInt(text, "FairyGUI.GTextField", "autoSize", 0);
            FairyBridge.SetBool(text, "FairyGUI.GTextField", "singleLine", desired <= width);
            return width;
        }

    }
}
