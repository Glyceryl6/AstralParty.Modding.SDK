using System;
using System.Runtime.InteropServices;
using IL2CppSharp;

namespace AstralParty.Modding.SDK.Native {
    public static class NativePackageBinding {
        [DllImport("GameAssembly", EntryPoint = "il2cpp_class_get_type", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr ClassType(IntPtr klass);
        [DllImport("GameAssembly", EntryPoint = "il2cpp_type_get_object", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr TypeObject(IntPtr type);
        public static void Register(string url, string type) {
            IntPtr nativeType = TypeObject(ClassType(FairyBridge.Class(type)));
            using var lease = new NativeObjectLease(nativeType);
            IntPtr text = NativePreviewList.Text(url);
            using var textLease = new NativeObjectLease(text);
            IntPtr method = Il2CppReflection.FindMethod(FairyBridge.Class("FairyGUI.UIObjectFactory"), "SetPackageItemExtension",
                new[] { FairyBridge.Class("System.String"), FairyBridge.Class("System.Type") });
            if (method == IntPtr.Zero || nativeType == IntPtr.Zero) throw new MissingMethodException("UIObjectFactory.SetPackageItemExtension(String, Type)");
            Il2CppReflection.InvokeMethod(method, IntPtr.Zero, new[] { text, nativeType });
        }
    }
}
