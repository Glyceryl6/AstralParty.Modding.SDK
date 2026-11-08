using System;
using AstralParty.Modding.SDK;
using IL2CppSharp;

using AstralParty.Modding.SDK.Native.UI;

namespace AstralParty.Modding.SDK.Native.Interop {
    /// <summary>
    /// Version-specific FairyGUI adapter for the pure managed NativeUiLayout.
    /// Keep the icon at the right edge, preserve full Chinese labels, and
    /// resize hit bounds with the artwork instead of shrinking fonts.
    /// 原生指针仅存在宿主；SDK 负责可复用的排版算法。
    /// </summary>
    public static class NativeToggleLayout {
        public static float PreferredWidth(IntPtr toggle, string label, int fontSize) {
            float nativeWidth = FairyBridge.GetFloat(toggle, "FairyGUI.GObject", "width");
            return NativeUiLayout.ToggleWidth(label, fontSize, nativeWidth);
        }

        public static void Fit(IntPtr toggle, string label, int fontSize, float width,
            float height) {
            IntPtr text = Il2CppReflection.InvokeMethod(
                FairyBridge.Method("FairyGUI.GButton", "GetTextField", 0),
                toggle, Array.Empty<IntPtr>());
            if (text == IntPtr.Zero) {
                throw new InvalidOperationException("Native toggle caption is missing");
            }
            IntPtr bg = FairyBridge.FieldValue(toggle, "UI.UISetting_Com_Toggle", "bg");
            IntPtr open = FairyBridge.FieldValue(toggle, "UI.UISetting_Com_Toggle", "open");
            // Apply the button bounds first, then anchor the native icon to
            // their RIGHT edge in absolute coordinates. The old delta-based
            // adjustment accumulated offsets and selected-state gears could
            // restore the icon without restoring its original hit region.
            FairyBridge.SetSize(toggle, width, height);
            float iconWidth = bg == IntPtr.Zero ? 48f :
            FairyBridge.GetFloat(bg, "FairyGUI.GObject", "width");
            float iconX = Math.Max(0f, width - iconWidth - 12f);
            if (bg != IntPtr.Zero) {
                FairyBridge.SetXY(bg, iconX,
                    (height - FairyBridge.GetFloat(bg, "FairyGUI.GObject", "height")) * 0.5f);
            }
            if (open != IntPtr.Zero) {
                float openWidth = FairyBridge.GetFloat(open, "FairyGUI.GObject", "width");
                // Center overlay artwork on the pink circle in both states.
                float openX = iconX + (iconWidth - openWidth) * 0.5f;
                FairyBridge.SetXY(open, openX,
                    (height - FairyBridge.GetFloat(open, "FairyGUI.GObject", "height")) * 0.5f);
            }
            float left = Math.Max(6f, FairyBridge.GetFloat(text, "FairyGUI.GObject", "x"));
            float textWidth = Math.Max(72f, iconX - left - 12f);
            float contentHeight = height;
            FairyBridge.SetSize(text, textWidth, contentHeight);
            FairyBridge.SetXY(text, left, 0f);
            FairyBridge.SetInt(text, "FairyGUI.GTextField", "verticalAlign", 1);
            FairyBridge.SetInt(text, "FairyGUI.GTextField", "autoSize",
                (int)NativeTextAutoSize.None);
            // Shorten a row only by wrapping when absolutely necessary.
            // 不缩小字号；若可用宽度太窄，则允许换行而不是截掉“数”等末字。
            FairyBridge.SetBool(text, "FairyGUI.GTextField", "singleLine",
                NativeUiLayout.EstimateCaptionWidth(label, fontSize) <= textWidth);
            FairyBridge.SetString(toggle, "FairyGUI.GButton", "title", label);
            FairyBridge.SetString(toggle, "FairyGUI.GButton", "selectedTitle", label);
            FairyBridge.SetInt(toggle, "FairyGUI.GButton", "titleFontSize", fontSize);
            NativeFairyText.ApplyStyle(text, fontSize, 0x202020);
            FairyBridge.SetBool(text, "FairyGUI.GObject", "touchable", false);
            NativeToggleControl.EnsureFullRectHitArea(toggle);
        }
    }
}
