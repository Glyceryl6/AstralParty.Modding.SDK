using AstralParty.Modding.SDK;
using System;
using System.Runtime.InteropServices;
using IL2CppSharp;


namespace AstralParty.Modding.SDK.Native {
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
        /// <summary>调用原生 getter 并返回对象指针；类型/方法解析失败时会报错。</summary>
        public static IntPtr GetObject(IntPtr obj, string type, string name) => Invoke(obj, type, "get_" + name, 0, null);
        /// <summary>调用返回非托管值的原生 getter，按 C# T 的大小读取结果。</summary>
        public static T Get<T>(IntPtr obj, string type, string name) where T : unmanaged {
            IntPtr boxed = GetObject(obj, type, name);
            if (boxed == IntPtr.Zero) throw new InvalidOperationException("Missing native value: " + type + "." + name);
            return Il2CppNative.Unbox<T>(boxed);
        }
        public static string GetString(IntPtr obj, string type, string name) =>
            Il2CppNative.ReadString(GetObject(obj, type, name));
        /// <summary>按 IL2CPP 参数 ABI 调用原生 setter；仅支持非托管实参。</summary>
        public static void Set<T>(IntPtr obj, string type, string name, T value) where T : unmanaged {
            IntPtr* args = stackalloc IntPtr[1];
            args[0] = (IntPtr)(&value);
            Invoke(obj, type, "set_" + name, 1, args);
        }
        /// <summary>将托管字符串封送为原生字符串后调用 setter。</summary>
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
