using System;
using System.Threading;
using AstralParty.Modding.SDK;

namespace AstralParty.Modding.SDK.Native.Interop {
    public sealed class NativeObjectLease : IDisposable {
        private readonly Il2CppNative.ObjectLease _lease;
        private int _released;
        private static long _activeCount;
        public static long ActiveCount => Interlocked.Read(ref _activeCount);
        public NativeObjectLease(IntPtr obj) {
            _lease = new Il2CppNative.ObjectLease(obj);
            Interlocked.Increment(ref _activeCount);
        }
        public void Dispose() {
            if (Interlocked.Exchange(ref _released, 1) != 0) return;
            try { _lease.Dispose(); }
            finally { Interlocked.Decrement(ref _activeCount); }
        }
    }
}
