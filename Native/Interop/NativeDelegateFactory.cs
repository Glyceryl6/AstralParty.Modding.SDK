using System;
using System.Runtime.InteropServices;
using IL2CppSharp;

using AstralParty.Modding.SDK.Native.UI;

namespace AstralParty.Modding.SDK.Native.Interop {
    /// <summary>Bind an existing native instance method; no injected managed callbacks or code detours.</summary>
    public static class NativeDelegateFactory {
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_class_get_type")]
        private static extern IntPtr ClassType(IntPtr klass);
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_type_get_object")]
        private static extern IntPtr TypeObject(IntPtr type);
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_method_get_object")]
        private static extern IntPtr MethodObject(IntPtr method, IntPtr referenceClass);

        public static IntPtr Bind(string delegateType, IntPtr target, string method, int arity) {
            if (target == IntPtr.Zero) throw new ArgumentNullException(nameof(target));
            IntPtr klass = Il2CppReflection.GetObjectClass(target);
            IntPtr routine = Il2CppReflection.FindMethodInHierarchy(klass, method, arity);
            if (routine == IntPtr.Zero) throw new MissingMethodException(method);
            IntPtr type = TypeObject(ClassType(FairyBridge.Class(delegateType)));
            using var typeLease = new NativeObjectLease(type);
            IntPtr reflected = MethodObject(routine, klass);
            using var methodLease = new NativeObjectLease(reflected);
            if (type == IntPtr.Zero || reflected == IntPtr.Zero) throw new InvalidOperationException("Native reflection metadata unavailable");
            IntPtr create = Il2CppReflection.FindMethod(FairyBridge.Class("System.Delegate"), "CreateDelegate",
                new[] { FairyBridge.Class("System.Type"), FairyBridge.Class("System.Object"), FairyBridge.Class("System.Reflection.MethodInfo") });
            if (create == IntPtr.Zero) throw new MissingMethodException("Delegate.CreateDelegate(Type, object, MethodInfo)");
            IntPtr callback = Il2CppReflection.InvokeMethod(create, IntPtr.Zero, new[] { type, target, reflected });
            if (callback == IntPtr.Zero) throw new InvalidOperationException("Native delegate binding failed: " + method);
            return callback;
        }
    }
}
