using AstralParty.Modding.SDK;
using System;
using System.Runtime.InteropServices;
using IL2CppSharp;


namespace AstralParty.Modding.SDK.Native {
    /// <summary>
    /// Shared, strictly typed IL2CPP native-value helpers. Never read an int/float
    /// as an object pointer; an invalid native field signature can crash Unity.
    /// 通用 IL2CPP 值类型访问：字段类型必须先在同版本热更新源码确认。
    /// </summary>
    public static class NativeValue {
        // IL2CPP typed field handling belongs to the SDK. Keep this facade
        // for existing MOD call sites and retain game-specific collection
        // reflection below until it can be exposed through a host adapter.
        private static readonly NativeTypedFields Fields = new(FairyBridge.Field);
        /// <summary>按类型与字段名读取原生布尔值；元数据解析由 SDK 缓存处理。</summary>
        public static bool ReadBool(IntPtr obj, string type, string name) => Fields.ReadBool(obj, type, name);
        /// <summary>按类型与字段名读取原生整数，错误类型会拒绝读取。</summary>
        public static int ReadInt(IntPtr obj, string type, string name) => Fields.ReadInt(obj, type, name);
        /// <summary>按类型与字段名读取原生浮点数。</summary>
        public static float ReadFloat(IntPtr obj, string type, string name) => Fields.ReadFloat(obj, type, name);
        /// <summary>写入经过字段类型校验的原生布尔值。</summary>
        public static void WriteBool(IntPtr obj, string type, string name, bool value) => Fields.WriteBool(obj, type, name, value);
        /// <summary>写入经过字段类型校验的原生整数。</summary>
        public static void WriteInt(IntPtr obj, string type, string name, int value) => Fields.WriteInt(obj, type, name, value);
        /// <summary>写入经过字段类型校验的原生浮点数。</summary>
        public static void WriteFloat(IntPtr obj, string type, string name, float value) => Fields.WriteFloat(obj, type, name, value);
        public static int ListCount(IntPtr list) => list == IntPtr.Zero ? 0 :
            Il2CppReflection.GetPropertyValue<int>(list, Il2CppReflection.GetObjectClass(list), "Count");
        public static IntPtr ListItem(IntPtr list, int index) => Il2CppReflection.InvokeMethod(
            Il2CppReflection.FindMethodInHierarchy(Il2CppReflection.GetObjectClass(list), "get_Item", 1),
            list, new[] { Il2CppReflection.BoxValue(FairyBridge.Class("System.Int32"), index) });
        public static int ListInt(IntPtr list, int index) => Il2CppReflection.InvokeGet<int>(
            Il2CppReflection.FindMethodInHierarchy(Il2CppReflection.GetObjectClass(list), "get_Item", 1),
            list, new[] { Il2CppReflection.BoxValue(FairyBridge.Class("System.Int32"), index) });
    }
}
