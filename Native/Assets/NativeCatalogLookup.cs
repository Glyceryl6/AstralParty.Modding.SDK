#nullable enable
using System;
using AstralParty.Modding.SDK;
using IL2CppSharp;

namespace AstralParty.Modding.SDK.Native {
    /// <summary>Checked protobuf map/localization access: never calls get_Item on missing keys.</summary>
    public static class NativeCatalogLookup {
        public static IntPtr GetById(string catalog, string type, string property, int id) {
            if (id <= 0) return IntPtr.Zero;
            IntPtr config = IL2CPP.Class("StaticConfigure")
                .Method("get_" + catalog).Args().InvokeStatic();
            if (config == IntPtr.Zero) return IntPtr.Zero;
            IntPtr dict = FairyBridge.GetObject(config, type, property);
            return GetDictionaryItem(dict, id);
        }

        public static bool HasLocal(string table, int id) {
            return GetById("STR" + table, "STR" + table + "Configure", "LocalDict", id) != IntPtr.Zero;
        }

        public static IntPtr GetDictionaryItem(IntPtr dict, int id) {
            if (dict == IntPtr.Zero || id <= 0) return IntPtr.Zero;
            IntPtr klass = Il2CppReflection.GetObjectClass(dict);
            IntPtr contains = Il2CppReflection.FindMethodInHierarchy(klass, "ContainsKey", 1);
            IntPtr get = Il2CppReflection.FindMethodInHierarchy(klass, "get_Item", 1);
            if (contains == IntPtr.Zero || get == IntPtr.Zero) return IntPtr.Zero;
            IntPtr boxed = Il2CppReflection.BoxValue(FairyBridge.Class("System.Int32"), id);
            IntPtr[] args = { boxed };
            if (!Il2CppReflection.InvokeGet<bool>(contains, dict, args)) {
                return IntPtr.Zero;
            }
            return Il2CppReflection.InvokeMethod(get, dict, args);
        }

        public static bool ValidityTime(IntPtr entry, string type, string begin, string end) {
            IntPtr start = FairyBridge.GetObject(entry, type, begin);
            IntPtr finish = FairyBridge.GetObject(entry, type, end);
            IntPtr stamp = FairyBridge.ClassOrZero("Google.Protobuf.WellKnownTypes.Timestamp");
            IntPtr time = FairyBridge.ClassOrZero("Tools.TimeHelper");
            if (stamp == IntPtr.Zero || time == IntPtr.Zero) return false;
            IntPtr check = Il2CppReflection.FindMethod(time, "ValidityTime", new[] { stamp, stamp });
            return check != IntPtr.Zero && Il2CppReflection.InvokeGet<bool>(check, IntPtr.Zero,
                new[] { start, finish }); // Match native server time and strict boundaries.
        }


    }
}

