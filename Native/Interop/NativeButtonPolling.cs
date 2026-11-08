#nullable enable
using System;
using System.Runtime.InteropServices;
using AstralParty.Modding.SDK.Native.UI;

namespace AstralParty.Modding.SDK.Native.Interop {
    /// <summary>Borrow an existing button's click without detouring shared native code.</summary>
    public sealed class NativeButtonPolling : IDisposable {
        public readonly IntPtr Button;
        private readonly NativeObjectLease _lease;
        private readonly NativeEventSuspension _events;
        private readonly int _mode;
        private readonly bool _change, _opaque, _touchable, _selected;
        private bool _disposed;
        [DllImport("GameAssembly", EntryPoint = "il2cpp_field_set_value", CallingConvention = CallingConvention.Cdecl)]
        private static extern void WriteByte(IntPtr obj, IntPtr field, ref byte value);

        public NativeButtonPolling(IntPtr button) {
            Button = button;
            _lease = new NativeObjectLease(button);
            _mode = FairyBridge.GetInt(button, "FairyGUI.GButton", "mode");
            _change = NativeValue.ReadBool(button, "FairyGUI.GButton", "changeStateOnClick");
            _opaque = FairyBridge.GetBool(button, "FairyGUI.GComponent", "opaque");
            _touchable = FairyBridge.GetBool(button, "FairyGUI.GObject", "touchable");
            _selected = FairyBridge.GetBool(button, "FairyGUI.GButton", "selected");
            _events = new NativeEventSuspension(FairyBridge.GetObject(button, "FairyGUI.GObject", "onClick"));
            try {
                NativeToggleControl.EnableSelectionOnClick(button);
                FairyBridge.SetBool(button, "FairyGUI.GButton", "selected", false);
            } catch { Dispose(); throw; }
        }

        public bool Consume() {
            _events.Suppress(); // Renderers can replace their native callback.
            if (!FairyBridge.GetBool(Button, "FairyGUI.GButton", "selected")) return false;
            FairyBridge.SetBool(Button, "FairyGUI.GButton", "selected", false);
            return true;
        }

        // Called only after a native owner is already gone. Never read from
        // or restore properties of the disposed GButton.
        public void Abandon() {
            if (_disposed) return;
            _disposed = true;
            _events.Abandon();
            _lease.Dispose();
        }

        public void Dispose() {
            if (_disposed) return;
            _disposed = true;
            try {
                if (!FairyBridge.GetBool(Button, "FairyGUI.GObject", "isDisposed")) {
                    _events.Dispose();
                    FairyBridge.SetInt(Button, "FairyGUI.GButton", "mode", _mode);
                    byte value = _change ? (byte)1 : (byte)0;
                    WriteByte(Button, FairyBridge.Field("FairyGUI.GButton", "changeStateOnClick"), ref value);
                    FairyBridge.SetBool(Button, "FairyGUI.GComponent", "opaque", _opaque);
                    if (FairyBridge.GetBool(Button, "FairyGUI.GObject", "touchable"))
                        FairyBridge.SetBool(Button, "FairyGUI.GObject", "touchable", _touchable);
                    FairyBridge.SetBool(Button, "FairyGUI.GButton", "selected", _selected);
                } else _events.Abandon();
            } finally { _lease.Dispose(); }
        }
    }
}
