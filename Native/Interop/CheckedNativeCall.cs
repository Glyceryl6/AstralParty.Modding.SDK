using AstralParty.Modding.SDK;
using System;
using System.Runtime.InteropServices;
using IL2CppSharp;

using AstralParty.Modding.SDK.Native.UI;

namespace AstralParty.Modding.SDK.Native.Interop {
    /// <summary>Instance property calls with null checks and surfaced native exceptions.</summary>
    public static unsafe class CheckedNativeCall {
        private static IntPtr Invoke(IntPtr obj, string type, string method, int arity, IntPtr* args) {
            // Never submit a null this-pointer to il2cpp_runtime_invoke.
            if (obj == IntPtr.Zero) throw new InvalidOperationException("Native instance is not ready: " + type + "." + method);
            IntPtr routine = FairyBridge.Method(type, method, arity);
            if (routine == IntPtr.Zero) throw new MissingMethodException(type, method);
            try { return Il2CppNative.InvokeInstance(routine, obj, FairyBridge.Class(type), args); }
            catch (InvalidOperationException ex) { throw new InvalidOperationException(type + "." + method + ": " + ex.Message, ex); }
        }
        public static IntPtr GetObject(IntPtr obj, string type, string name) => Invoke(obj, type, "get_" + name, 0, null);
        public static T Get<T>(IntPtr obj, string type, string name) where T : unmanaged {
            IntPtr boxed = GetObject(obj, type, name);
            if (boxed == IntPtr.Zero) throw new InvalidOperationException("Missing native value: " + type + "." + name);
            return Il2CppNative.Unbox<T>(boxed);
        }
        public static string GetString(IntPtr obj, string type, string name) =>
            Il2CppNative.ReadString(GetObject(obj, type, name));
        public static void Set<T>(IntPtr obj, string type, string name, T value) where T : unmanaged {
            IntPtr* args = stackalloc IntPtr[1];
            args[0] = (IntPtr)(&value);
            Invoke(obj, type, "set_" + name, 1, args);
        }
        public static void SetString(IntPtr obj, string type, string name, string value) {
            if (obj == IntPtr.Zero) throw new InvalidOperationException("Native instance is not ready: " + type + ".set_" + name);
            IntPtr text = Il2CppNative.NewString(value ?? "");
            using var lease = new NativeObjectLease(text);
            IntPtr* args = stackalloc IntPtr[1];
            args[0] = text; // Reference arguments are object pointers, not boxed value buffers.
            Invoke(obj, type, "set_" + name, 1, args);
        }
    }
}
