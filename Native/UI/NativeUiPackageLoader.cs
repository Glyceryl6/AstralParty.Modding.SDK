#nullable enable
using AstralParty.Modding.SDK.Native;
using System;
using IL2CppSharp;
using UnityEngine;
using AstralParty.Modding.SDK.Native.Interop;

namespace AstralParty.Modding.SDK.Native.UI {
    public sealed class NativeUiPackageLoader {
        private readonly string _name;
        private NativeBoxedTask? _load;
        private NativeObjectLease? _loadLease;
        private float _retryAt;
        private bool _metadataUnavailable;
        public NativeUiPackageLoader(string name) { _name = name; }
        public bool Ready() {
            if (_metadataUnavailable || Time.realtimeSinceStartup < _retryAt) return false;
            try {
                if (_load != null) {
                    if (!_load.TryComplete(out bool success)) return false;
                    _load.Dispose();
                    _loadLease?.Dispose();
                    _load = null;
                    _loadLease = null;
                    if (!success) { _retryAt = Time.realtimeSinceStartup + 10f; return false; }
                }
                IntPtr package = Il2CppReflection.InvokeMethodWithString(FairyBridge.Method("FairyGUI.UIPackage", "GetByName", 1), IntPtr.Zero, _name);
                if (package != IntPtr.Zero) return true;
                IntPtr task = Il2CppReflection.InvokeMethod(FairyBridge.Method("FairyGUI.UIPackage", "AddPackageAsync", 2), IntPtr.Zero,
                    new[] { NativePreviewList.Text(_name), NativePreviewList.Text("") });
                _loadLease = new NativeObjectLease(task);
                try { _load = new NativeBoxedTask(task, expectsBoolean: false); }
                catch { _metadataUnavailable = true; throw; }
                return false;
            } catch (Exception ex) {
                // Keep an unobservable in-flight load rooted; do not start duplicates.
                if (_load != null && _load.Consumed) {
                    _load.Dispose();
                    _loadLease?.Dispose();
                    _load = null;
                    _loadLease = null;
                }
                _retryAt = Time.realtimeSinceStartup + 10f;
                NativeSdkServices.Warn("Native UI package deferred: " + ex.Message);
                return false;
            }
        }
    }
}
