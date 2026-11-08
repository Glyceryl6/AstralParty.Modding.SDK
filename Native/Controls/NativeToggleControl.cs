using System;
using System.Runtime.InteropServices;
using IL2CppSharp;
using AstralParty.Modding.SDK;


namespace AstralParty.Modding.SDK.Native {
    /// <summary>
    /// Shared native GButton setup for FairyGUI toggles.
    /// 复用原生按钮初始化，避免不同模块分别写入不兼容的 IL2CPP byte 字段。
    /// </summary>
    public static class NativeToggleControl {
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "il2cpp_field_set_value")]
        private static extern void SetNativeByteField(IntPtr instance, IntPtr field, ref byte value);

        /// <summary>确保复选按钮的交互区域覆盖视觉边框，适合原生 FairyGUI 控件。</summary>
        public static void EnsureFullRectHitArea(IntPtr button) {
            if (button == IntPtr.Zero) {
                return;
            }
            // Crash-isolation hotfix: rely only on FairyGUI's public properties.
            // Avoid il2cpp_field_set_value(Container.hitArea) entirely until the
            // exact runtime object layout has been verified with a native dump.
            // 先禁用直接写入原生 IHitTest 字段；点击区域偏移可能暂时恢复旧行为。
            FairyBridge.SetBool(button, "FairyGUI.GObject", "touchable", true);
            FairyBridge.SetBool(button, "FairyGUI.GComponent", "opaque", true);
        }

        /// <summary>为原生复选按钮恢复点击选中行为；不自动持久化 MOD 配置。</summary>
        public static void EnableSelectionOnClick(IntPtr button) {
            FairyBridge.SetInt(button, "FairyGUI.GButton", "mode",
                (int)NativeButtonMode.Check);
            IntPtr field = FairyBridge.Field("FairyGUI.GButton", "changeStateOnClick");
            if (field == IntPtr.Zero) {
                throw new MissingFieldException("FairyGUI.GButton.changeStateOnClick");
            }
            byte enabled = 1;
            SetNativeByteField(button, field, ref enabled);
            EnsureFullRectHitArea(button);
        }
    }
}
