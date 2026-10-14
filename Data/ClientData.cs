using System;
using System.Collections.Generic;

namespace AstralParty.Modding.SDK;

/// <summary>
/// Reversible override of a client-side managed property. NEVER use it for
/// server-authoritative balances, rank, inventory, cooldowns or network packets.
/// 可撤销的纯客户端显示/配置值替换。不得用于伪造服务端授权数据。
/// </summary>
public interface IClientValueRegistry {
    bool Register<T>(string key, Func<T> read, Action<T> write, Func<T, bool>? validate = null);
    bool TryRead<T>(string key, out T value);
    bool TryOverride<T>(string key, T temporaryValue, out IDisposable? restoration);
}

/// <summary>
/// Host-owned registry. Registration does not expose native pointers; scope
/// restoration is conditional so another legitimate update isn't overwritten.
/// 租约只在当前值仍是自己的临时值时恢复，避免覆盖游戏后续正常更新。
/// </summary>
public sealed class ClientValueRegistry : IClientValueRegistry {
    private interface ISlot { }
    private sealed class Slot<T> : ISlot {
        public readonly Func<T> Read;
        public readonly Action<T> Write;
        public readonly Func<T, bool>? Validate;
        public bool ActiveLease;
        public Slot(Func<T> read, Action<T> write, Func<T, bool>? validate) {
            Read = read;
            Write = write;
            Validate = validate;
        }
    }

    private sealed class Lease<T> : IDisposable {
        private readonly ClientValueRegistry _registry;
        private readonly Slot<T> _slot;
        private readonly T _previous;
        private readonly T _temporary;
        private bool _disposed;
        public Lease(ClientValueRegistry registry, Slot<T> slot, T previous, T temporary) {
            _registry = registry;
            _slot = slot;
            _previous = previous;
            _temporary = temporary;
        }
        public void Dispose() {
            _registry.CheckThread();
            if (_disposed) {
                return;
            }
            try {
                if (EqualityComparer<T>.Default.Equals(_slot.Read(), _temporary)) {
                    _slot.Write(_previous);
                }
            }
            finally {
                _disposed = true;
                _slot.ActiveLease = false;
            }
        }
    }

    private readonly Dictionary<string, ISlot> _slots = new(StringComparer.Ordinal);
    private readonly Func<bool> _onMainThread;
    public ClientValueRegistry(Func<bool> onMainThread) {
        _onMainThread = onMainThread ?? throw new ArgumentNullException(nameof(onMainThread));
    }

    private void CheckThread() {
        if (!_onMainThread()) {
            throw new InvalidOperationException("SDK client values can only be accessed on the game main thread.");
        }
    }

    public bool Register<T>(string key, Func<T> read, Action<T> write, Func<T, bool>? validate = null) {
        CheckThread();
        if (string.IsNullOrWhiteSpace(key) || read == null || write == null) {
            return false;
        }
        if (_slots.ContainsKey(key)) {
            return false;
        }
        _slots.Add(key, new Slot<T>(read, write, validate));
        return true;
    }

    public bool TryRead<T>(string key, out T value) {
        CheckThread();
        if (_slots.TryGetValue(key, out ISlot? slot) && slot is Slot<T> typed) {
            value = typed.Read();
            return true;
        }
        value = default!;
        return false;
    }

    public bool TryOverride<T>(string key, T temporaryValue, out IDisposable? restoration) {
        CheckThread();
        restoration = null;
        if (!_slots.TryGetValue(key, out ISlot? slot) || slot is not Slot<T> typed ||
            typed.ActiveLease ||
            (typed.Validate != null && !typed.Validate(temporaryValue))) {
            return false;
        }
        T previous = typed.Read();
        typed.Write(temporaryValue);
        typed.ActiveLease = true;
        restoration = new Lease<T>(this, typed, previous, temporaryValue);
        return true;
    }
}
