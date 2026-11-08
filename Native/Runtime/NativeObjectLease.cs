using System;
using System.Threading;
using AstralParty.Modding.SDK;

namespace AstralParty.Modding.SDK.Native {
    /// <summary>借助 Il2CppNative.ObjectLease 临时保护原生对象，必须及时释放；不能保证对象仍属于当前场景。</summary>
    public sealed class NativeObjectLease : IDisposable {
        private readonly Il2CppNative.ObjectLease _lease;
        private int _released;
        private static long _activeCount;
        /// <summary>当前仍持有的租约数量，仅供排查泄漏，不应作为游戏状态依据。</summary>
        public static long ActiveCount => Interlocked.Read(ref _activeCount);
        public NativeObjectLease(IntPtr obj) {
            _lease = new Il2CppNative.ObjectLease(obj);
            Interlocked.Increment(ref _activeCount);
        }
        /// <summary>释放本租约关联的 GCHandle；避免跨帧或跨场景长期持有。</summary>
        public void Dispose() {
            if (Interlocked.Exchange(ref _released, 1) != 0) return;
            try { _lease.Dispose(); }
            finally { Interlocked.Decrement(ref _activeCount); }
        }
    }
}
