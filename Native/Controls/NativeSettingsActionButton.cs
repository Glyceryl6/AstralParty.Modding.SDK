using System;
using AstralParty.Modding.SDK;
using IL2CppSharp;

namespace AstralParty.Modding.SDK.Native {
    public static class NativeSettingsActionButton {
        public static IntPtr Create(IntPtr parent, string caption, float width, float height, out IntPtr text, bool nativeScale = false) {
            // Use the actual redemption action, not the left settings-tab prefab.
            IntPtr template = Il2CppReflection.InvokeMethod(FairyBridge.Method("UI.UISetting_Com_User", "CreateInstance", 0), IntPtr.Zero, Array.Empty<IntPtr>());
            IntPtr button = IntPtr.Zero;
            float authoredWidth = 0f, authoredHeight = 0f, authoredScale = 1f;
            if (template != IntPtr.Zero) {
                using var lease = new NativeObjectLease(template);
                try {
                    IntPtr source = FairyBridge.FieldValue(template, "UI.UISetting_Com_User", "btn_CDK");
                    string url = source == IntPtr.Zero ? "" : FairyBridge.GetString(source, "FairyGUI.GObject", "resourceURL");
                    if (url.Length > 0) button = Il2CppReflection.InvokeMethodWithString(FairyBridge.Method("FairyGUI.UIPackage", "CreateObjectFromURL", 1), IntPtr.Zero, url);
                    if (button != IntPtr.Zero && nativeScale) {
                        using var cloneLease = new NativeObjectLease(button);
                        authoredWidth = FairyBridge.GetFloat(source, "FairyGUI.GObject", "width");
                        authoredHeight = FairyBridge.GetFloat(source, "FairyGUI.GObject", "height");
                        authoredScale = Math.Min(FairyBridge.GetFloat(source, "FairyGUI.GObject", "scaleX"), FairyBridge.GetFloat(source, "FairyGUI.GObject", "scaleY"));
                        IntPtr sourceText = NativeReferenceFields.Call(source, "GetTextField");
                        IntPtr cloneText = NativeReferenceFields.Call(button, "GetTextField");
                        IntPtr sourceFormat = FairyBridge.GetObject(sourceText, "FairyGUI.GTextField", "textFormat");
                        IntPtr cloneFormat = FairyBridge.GetObject(cloneText, "FairyGUI.GTextField", "textFormat");
                        NativeReferenceFields.Call(cloneFormat, "CopyFrom", sourceFormat);
                        NativeReferenceFields.Call(cloneText, "set_textFormat", cloneFormat);
                    }
                } finally { NativeReferenceFields.Call(template, "Dispose"); }
            }
            if (button == IntPtr.Zero) button = FairyBridge.PackageItem("Setting", "Setting_Com_Button");
            if (button == IntPtr.Zero) throw new InvalidOperationException("Native settings action button is not ready");
            using var buttonLease = new NativeObjectLease(button);
            try {
                NativeToggleControl.EnableSelectionOnClick(button);
                FairyBridge.SetBool(button, "FairyGUI.GButton", "selected", false);
                FairyBridge.SetString(button, "FairyGUI.GButton", "title", NativeSdkServices.Translate(caption));
                FairyBridge.SetString(button, "FairyGUI.GButton", "selectedTitle", "");
                text = NativeReferenceFields.Call(button, "GetTextField");
                StableNativeAction.Prepare(button, text);
                float nativeWidth = Math.Max(1f, FairyBridge.GetFloat(button, "FairyGUI.GObject", "width"));
                float nativeHeight = Math.Max(1f, FairyBridge.GetFloat(button, "FairyGUI.GObject", "height"));
                if (nativeScale && authoredWidth > 0f && authoredHeight > 0f) {
                    FairyBridge.SetSize(button, authoredWidth, authoredHeight);
                    nativeWidth = authoredWidth;
                    nativeHeight = authoredHeight;
                }
                float scale = nativeScale ? Math.Min(Math.Max(.1f, authoredScale), width / nativeWidth) :
                    NativeUiLayout.UniformScale(nativeWidth, nativeHeight, width, height);
                // Uniform scale preserves the native corners, border and text proportions.
                NativeReferenceFields.Call(button, "SetScale", StableNativeAction.Float(scale), StableNativeAction.Float(scale));
                FairyBridge.AddChild(parent, button);
                return button;
            } catch { NativeReferenceFields.Call(button, "Dispose"); throw; }
        }
    }
}
