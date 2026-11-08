using System;
using System.Runtime.InteropServices;
using IL2CppSharp;


namespace AstralParty.Modding.SDK.Native {
    public static class NativeModeListScroller {
        // ByteBuffer/ScrollPane.Setup layout verified against the attached Runtime.
        // Vertical, visible native scrollbar, demand display + touch + no bounce.
        private static readonly byte[] ScrollData = [
            1, 1, 0, 0, 0, 148, 0,
            255, 254, 255, 254, 255, 254, 255, 254
        ];

        [DllImport("GameAssembly", EntryPoint = "il2cpp_field_static_set_value", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetStaticReference(IntPtr field, ref IntPtr value);

        public static void Ensure(IntPtr list, IntPtr roomUi) {
            if (list == IntPtr.Zero) return;
            if (roomUi != IntPtr.Zero) {
                float roomHeight = FairyBridge.GetFloat(roomUi, "FairyGUI.GObject", "height");
                float y = FairyBridge.GetFloat(list, "FairyGUI.GObject", "y");
                float height = FairyBridge.GetFloat(list, "FairyGUI.GObject", "height");
                FairyBridge.SetFloat(list, "FairyGUI.GObject", "height", Math.Min(height, Math.Max(140f, roomHeight - y - 36f)));
            }
            IntPtr pane = FairyBridge.GetObject(list, "FairyGUI.GComponent", "scrollPane");
            if (pane == IntPtr.Zero) {
                // Return existing cells before moving the list's display container.
                // The enclosing RefreshGameMode immediately runs the native renderer.
                FairyBridge.SetInt(list, "FairyGUI.GList", "numItems", 0);
                IntPtr buffer = Il2CppAPI.CreateObject(FairyBridge.Class("FairyGUI.Utils.ByteBuffer"));
                using var bufferRoot = new NativeObjectLease(buffer);
                IL2CPP.Class("FairyGUI.Utils.ByteBuffer").Method(".ctor")
                    .Args<byte[], int, int>().Invoke(buffer, ScrollData, 0, ScrollData.Length);
                IntPtr field = FairyBridge.Field("FairyGUI.UIConfig", "verticalScrollBar");
                IntPtr original = Il2CppReflection.GetStaticFieldValue(FairyBridge.Class("FairyGUI.UIConfig"), "verticalScrollBar");
                IntPtr borrowed = original == IntPtr.Zero && roomUi != IntPtr.Zero ? FindRoomScrollBarUrl(roomUi) : IntPtr.Zero;
                using var originalRoot = new NativeObjectLease(original);
                using var borrowedRoot = new NativeObjectLease(borrowed);
                try {
                    // Reuse the room popup's native artwork when UIConfig has no default bar.
                    if (borrowed != IntPtr.Zero) SetStaticReference(field, ref borrowed);
                    Il2CppReflection.InvokeMethod(FairyBridge.Method("FairyGUI.GComponent", "SetupScroll", 1),
                        list, [buffer]);
                } finally {
                    if (borrowed != IntPtr.Zero) SetStaticReference(field, ref original);
                }
                pane = FairyBridge.GetObject(list, "FairyGUI.GComponent", "scrollPane");
            }
            if (pane == IntPtr.Zero) return;
            FairyBridge.SetBool(list, "FairyGUI.GComponent", "opaque", true);
            IntPtr bar = FairyBridge.GetObject(pane, "FairyGUI.ScrollPane", "vtScrollBar");
            if (bar != IntPtr.Zero) FairyBridge.SetBool(bar, "FairyGUI.GObject", "visible", true);
            FairyBridge.SetBool(pane, "FairyGUI.ScrollPane", "mouseWheelEnabled", true);
            FairyBridge.SetBool(pane, "FairyGUI.ScrollPane", "touchEffect", true);
            FairyBridge.SetBool(pane, "FairyGUI.ScrollPane", "bouncebackEffect", false);
        }

        private static IntPtr FindRoomScrollBarUrl(IntPtr ui) {
            IntPtr create = FairyBridge.FieldValue(ui, "UI.UIRoomListPanel", "com_CreateRoom");
            IntPtr settings = FairyBridge.FieldValue(create, "UI.UICom_CreateRoom", "com_RoomSetting");
            IntPtr popup = FairyBridge.FieldValue(settings, "UI.UICom_RoomSetting", "_roomSetting_ComboBox_popup");
            IntPtr list = FairyBridge.FieldValue(popup, "UI.UIRoomSetting_ComboBox_popup", "list");
            IntPtr pane = FairyBridge.GetObject(list, "FairyGUI.GComponent", "scrollPane");
            IntPtr bar = FairyBridge.GetObject(pane, "FairyGUI.ScrollPane", "vtScrollBar");
            return FairyBridge.GetObject(bar, "FairyGUI.GObject", "resourceURL");
        }
    }
}
