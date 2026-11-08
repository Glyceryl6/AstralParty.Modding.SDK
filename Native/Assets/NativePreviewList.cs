using AstralParty.Modding.SDK;
using System;
using System.Runtime.InteropServices;
using IL2CppSharp;


namespace AstralParty.Modding.SDK.Native {
    /// <summary>
    /// Runtime List&lt;T&gt; operations against actual HybridCLR objects. Do not use
    /// a fabricated managed List or replace the game's itemRenderer callback.
    /// All calls are made on the Unity main thread.
    /// </summary>
    public static class NativePreviewList {
        public static void Add(IntPtr list, IntPtr item) {
            Invoke(list, "Add", new[] { item });
        }

        public static void Insert(IntPtr list, int index, IntPtr item) {
            Invoke(list, "Insert", new[] { Box(index), item });
        }

        public static void RemoveAt(IntPtr list, int index) {
            Invoke(list, "RemoveAt", new[] { Box(index) });
        }

        public static IntPtr Box(int value) =>
            Il2CppReflection.BoxValue(FairyBridge.Class("System.Int32"), value);

        public static IntPtr Enum(string type, int value) =>
            Il2CppReflection.BoxValue(FairyBridge.Class(type), value);

        public static IntPtr Text(string value) => Il2CppNative.NewString(value ?? string.Empty);

        public static IntPtr New(string type, params IntPtr[] parameters) {
            IntPtr instance = Il2CppAPI.CreateObject(FairyBridge.Class(type));
            if (instance == IntPtr.Zero) throw new InvalidOperationException("Cannot allocate " + type);
            IntPtr constructor = FairyBridge.TryMethod(type, ".ctor", parameters.Length);
            if (constructor == IntPtr.Zero) throw new MissingMethodException(type, ".ctor");
            Il2CppReflection.InvokeMethod(constructor, instance, parameters);
            return instance;
        }

        public static void Invoke(IntPtr instance, string method, IntPtr[] parameters) {
            if (instance == IntPtr.Zero) throw new ArgumentNullException(nameof(instance));
            IntPtr routine = Il2CppReflection.FindMethodInHierarchy(
                Il2CppReflection.GetObjectClass(instance), method, parameters.Length);
            if (routine == IntPtr.Zero) throw new MissingMethodException("native List<T>", method);
            Il2CppReflection.InvokeMethod(routine, instance, parameters);
        }

        public static void SetCount(IntPtr list, int count) {
            if (list != IntPtr.Zero && FairyBridge.GetInt(list, "FairyGUI.GList", "numItems") != count) {
                FairyBridge.SetInt(list, "FairyGUI.GList", "numItems", count);
            }
        }

        // A fixed-size emoji grid (usually 20 cells) needs its native itemRenderer
        // called again even if its logical count did not change.
        public static void Rebind(IntPtr list, int count) {
            if (list == IntPtr.Zero) return;
            if (count < 0 || count > 16384) throw new ArgumentOutOfRangeException(nameof(count));
            using var root = new NativeObjectLease(list);
            if (FairyBridge.GetBool(list, "FairyGUI.GObject", "isDisposed"))
                throw new InvalidOperationException("Cannot rebind a disposed native list");
            if (FairyBridge.GetBool(list, "FairyGUI.GList", "isVirtual") &&
                FairyBridge.FieldValue(list, "FairyGUI.GList", "itemRenderer") == IntPtr.Zero)
                throw new InvalidOperationException("Cannot rebind a virtual list without its renderer");
            FairyBridge.SetInt(list, "FairyGUI.GList", "numItems", 0);
            if (count > 0) FairyBridge.SetInt(list, "FairyGUI.GList", "numItems", count);
        }

        // Never remove anything from an unrelated list rebuilt by the game.
        public static int RemoveAppended(IntPtr list, System.Collections.Generic.HashSet<int> ids,
            Func<IntPtr, int> getId) {
            int removed = 0;
            int count = NativeValue.ListCount(list);
            if (count < 0 || count > 4096) return 0;
            for (int i = count - 1; i >= 0; i--) {
                IntPtr item = NativeValue.ListItem(list, i);
                if (item == IntPtr.Zero) continue;
                if (!ids.Contains(getId(item))) continue;
                RemoveAt(list, i);
                removed++;
            }
            ids.Clear();
            return removed;
        }
    }
}
