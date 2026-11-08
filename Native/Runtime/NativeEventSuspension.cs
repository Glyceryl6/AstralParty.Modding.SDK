using System;
using System.Collections.Generic;


namespace AstralParty.Modding.SDK.Native {
    /// <summary>Temporarily suspend an existing native event bridge without creating callbacks.</summary>
    public sealed class NativeEventSuspension : IDisposable {
        private static readonly string[] Fields = { "_callback0", "_callback1", "_captureCallback" };
        private readonly IntPtr _bridge;
        private readonly IntPtr[] _callbacks = new IntPtr[3];
        private readonly List<NativeObjectLease> _leases = new();
        private bool _disposed;
        public NativeEventSuspension(IntPtr listener) {
            _bridge = FairyBridge.FieldValue(listener, "FairyGUI.EventListener", "_bridge");
            if (_bridge == IntPtr.Zero) throw new InvalidOperationException("Native event bridge unavailable");
            _leases.Add(new NativeObjectLease(_bridge));
            Suppress();
        }
        public void Suppress() {
            if (_disposed) return;
            for (int i = 0; i < Fields.Length; i++) {
                IntPtr current = FairyBridge.FieldValue(_bridge, "FairyGUI.EventBridge", Fields[i]);
                if (current != IntPtr.Zero) {
                    _callbacks[i] = current; _leases.Add(new NativeObjectLease(current));
                    NativeReferenceFields.Set(_bridge, "FairyGUI.EventBridge", Fields[i], IntPtr.Zero);
                }
            }
        }
        // An event bridge belonging to a disposed FairyGUI panel must NOT be
        // written back to. Only release the retained native handles.
        public void Abandon() {
            if (_disposed) return;
            _disposed = true;
            foreach (var lease in _leases) lease.Dispose();
            _leases.Clear();
        }
        public void Dispose() {
            if (_disposed) return;
            try {
                for (int i = 0; i < Fields.Length; i++)
                    if (FairyBridge.FieldValue(_bridge, "FairyGUI.EventBridge", Fields[i]) == IntPtr.Zero)
                        NativeReferenceFields.Set(_bridge, "FairyGUI.EventBridge", Fields[i], _callbacks[i]);
            } finally {
                _disposed = true;
                foreach (var lease in _leases) lease.Dispose();
                _leases.Clear();
            }
        }
    }
}
