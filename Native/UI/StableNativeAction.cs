using System;
using IL2CppSharp;
using AstralParty.Modding.SDK.Native.Interop;

namespace AstralParty.Modding.SDK.Native.UI {
    /// <summary>Keep native artwork while removing state-dependent title, scale and text-size feedback.</summary>
    public static class StableNativeAction {
        public static void Prepare(IntPtr button, IntPtr text) {
            NativeValue.WriteInt(button, "FairyGUI.GButton", "_downEffect", 1);
            NativeMailActionButton.DetachSizeGears(button, 0);
            NativeReferenceFields.Call(button, "SetScale", Float(1f), Float(1f));
            string title = FairyBridge.GetString(button, "FairyGUI.GButton", "title");
            FairyBridge.SetString(button, "FairyGUI.GButton", "selectedTitle", title);
            foreach (int index in new[] { 0, 9 }) {
                IntPtr gear = NativeReferenceFields.Call(text, "GetGear", NativePreviewList.Box(index));
                if (gear != IntPtr.Zero) { NativeReferenceFields.Call(gear, "Dispose"); NativeReferenceFields.Call(gear, "set_controller", IntPtr.Zero); }
            }
            NativeReferenceFields.Call(text, "set_group", IntPtr.Zero);
            FairyBridge.SetBool(text, "FairyGUI.GObject", "visible", true);
            FairyBridge.SetInt(text, "FairyGUI.GTextField", "autoSize", 0);
            FairyBridge.SetBool(text, "FairyGUI.GTextField", "singleLine", true);
            NativeFairyText.ClearOutline(text);
        }
        public static IntPtr Float(float value) => Il2CppReflection.BoxValue(FairyBridge.Class("System.Single"), value);
        public static void SetCaption(IntPtr button, string title) {
            FairyBridge.SetString(button, "FairyGUI.GButton", "title", title);
            FairyBridge.SetString(button, "FairyGUI.GButton", "selectedTitle", title);
        }
    }
}
