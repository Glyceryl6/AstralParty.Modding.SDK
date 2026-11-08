using AstralParty.Modding.SDK;
using System;
using System.Runtime.InteropServices;
using IL2CppSharp;


namespace AstralParty.Modding.SDK.Native {
    public static class NativeReferenceFields {
        public static string ReadString(IntPtr value) => Il2CppNative.ReadString(value);
        public static void Set(IntPtr obj, string type, string name, IntPtr value) =>
            Il2CppNative.SetReference(obj, FairyBridge.Field(type, name), FairyBridge.Class(type), value);
        public static IntPtr Call(IntPtr obj, string method, params IntPtr[] args) {
            if (obj == IntPtr.Zero) throw new ArgumentNullException(nameof(obj));
            IntPtr routine = Il2CppReflection.FindMethodInHierarchy(Il2CppReflection.GetObjectClass(obj), method, args.Length);
            if (routine == IntPtr.Zero) throw new MissingMethodException(method);
            return Il2CppReflection.InvokeMethod(routine, obj, args);
        }
    }
}
