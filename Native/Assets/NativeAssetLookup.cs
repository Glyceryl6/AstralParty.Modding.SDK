using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using IL2CppSharp;


namespace AstralParty.Modding.SDK.Native {
    /// <summary>Queries loaded location maps only; never starts a download or asset load.</summary>
    public static class NativeAssetLookup {
        private static readonly HashSet<string> Found = new(StringComparer.Ordinal);
        [DllImport("GameAssembly", EntryPoint = "il2cpp_runtime_invoke", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe IntPtr RuntimeInvoke(IntPtr method, IntPtr obj, IntPtr* args, out IntPtr exception);

        public static unsafe bool Contains(string key) {
            if (string.IsNullOrWhiteSpace(key)) return false;
            if (Found.Contains(key)) return true;
            try {
                IntPtr getter = FairyBridge.TryMethod("UnityEngine.AddressableAssets.Addressables", "get_ResourceLocators", 0);
                if (getter == IntPtr.Zero) return false;
                IntPtr locators = Il2CppReflection.InvokeMethod(getter, IntPtr.Zero, Array.Empty<IntPtr>());
                IntPtr enumerator = Call(locators, "GetEnumerator");
                if (enumerator == IntPtr.Zero) return false;
                using var iteratorRoot = new NativeObjectLease(enumerator);
                try {
                    IntPtr move = Il2CppReflection.FindMethodInHierarchy(Il2CppReflection.GetObjectClass(enumerator), "MoveNext", 0);
                    if (move == IntPtr.Zero) return false;
                    IntPtr nativeKey = NativePreviewList.Text(key);
                    using var keyRoot = new NativeObjectLease(nativeKey);
                    IntPtr* args = stackalloc IntPtr[3];
                    for (int i = 0; i < 256 && Il2CppReflection.InvokeGet<bool>(move, enumerator, Array.Empty<IntPtr>()); i++) {
                        IntPtr locator = Call(enumerator, "get_Current");
                        if (locator == IntPtr.Zero) continue;
                        IntPtr klass = Il2CppReflection.GetObjectClass(locator);
                        // ResourceLocationMap is authoritative. LegacyResourcesLocator accepts
                        // arbitrary strings and therefore cannot prove that a key exists.
                        IntPtr expected = FairyBridge.ClassOrZero("UnityEngine.AddressableAssets.ResourceLocators.ResourceLocationMap");
                        if (klass != expected) continue;
                        IntPtr locate = Il2CppReflection.FindMethodInHierarchy(klass, "Locate", 3);
                        if (locate == IntPtr.Zero) continue;
                        IntPtr locations = IntPtr.Zero;
                        args[0] = nativeKey;
                        args[1] = IntPtr.Zero; // any resource type
                        args[2] = (IntPtr)(&locations); // out IList<IResourceLocation>, not boxed
                        IntPtr result = RuntimeInvoke(locate, locator, args, out IntPtr error);
                        if (error == IntPtr.Zero && result != IntPtr.Zero && locations != IntPtr.Zero &&
                            Il2CppReflection.UnboxValue<bool>(result)) {
                            Found.Add(key);
                            return true;
                        }
                    }
                } finally { Call(enumerator, "Dispose"); }
            } catch (Exception) { /* Late catalogs are retried; negative answers are never cached. */ }
            return false;
        }

        private static IntPtr Call(IntPtr obj, string name) {
            if (obj == IntPtr.Zero) return IntPtr.Zero;
            IntPtr method = Il2CppReflection.FindMethodInHierarchy(Il2CppReflection.GetObjectClass(obj), name, 0);
            return method == IntPtr.Zero ? IntPtr.Zero : Il2CppReflection.InvokeMethod(method, obj, Array.Empty<IntPtr>());
        }
    }
}
