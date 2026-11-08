using AstralParty.Modding.SDK.Native;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using IL2CppSharp;

using AstralParty.Modding.SDK.Native.UI;

namespace AstralParty.Modding.SDK.Native.Interop {
    /// <summary>
    /// Reads actual hot-update enum members from the loaded IL2CPP/HybridCLR image.
    /// No decompiled C# source, generated enum table or hotfix proxy is referenced.
    /// These eight game enums use 32-bit integer values in Runtime-20261016.
    /// </summary>
    public readonly struct RuntimeEnumEntry {
        public readonly int Id;
        public readonly string Name;

        public RuntimeEnumEntry(int id, string name) {
            Id = id;
            Name = name;
        }
    }

    public static class RuntimeEnumReflection {
        private const int MaxFields = 2048;
        private const int LiteralFlag = 0x0040;
        private static readonly IReadOnlyList<RuntimeEnumEntry> Empty = Array.Empty<RuntimeEnumEntry>();
        private static readonly Dictionary<string, IReadOnlyList<RuntimeEnumEntry>> Cache =
            new Dictionary<string, IReadOnlyList<RuntimeEnumEntry>>(StringComparer.Ordinal);
        private static readonly HashSet<string> Warned = new HashSet<string>(StringComparer.Ordinal);

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "il2cpp_class_is_enum")]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool ClassIsEnum(IntPtr klass);

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "il2cpp_class_get_fields")]
        private static extern IntPtr ClassGetFields(IntPtr klass, ref IntPtr iterator);

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "il2cpp_field_get_flags")]
        private static extern int FieldGetFlags(IntPtr field);

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "il2cpp_field_get_name")]
        private static extern IntPtr FieldGetName(IntPtr field);

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "il2cpp_field_get_value_object")]
        private static extern IntPtr FieldGetValueObject(IntPtr field, IntPtr instance);

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "il2cpp_object_unbox")]
        private static extern IntPtr ObjectUnbox(IntPtr boxed);

        public static IReadOnlyList<RuntimeEnumEntry> Entries(string typeName) {
            if (Cache.TryGetValue(typeName, out IReadOnlyList<RuntimeEnumEntry> existing)) {
                return existing;
            }
            if (!FairyBridge.Ready()) return Empty;
            IntPtr klass = FairyBridge.ClassOrZero(typeName);
            if (klass == IntPtr.Zero) return Empty;
            try {
                if (!ClassIsEnum(klass)) return Empty;
                var values = new List<RuntimeEnumEntry>();
                IntPtr iterator = IntPtr.Zero;
                for (int i = 0; i < MaxFields; i++) {
                    IntPtr field = ClassGetFields(klass, ref iterator);
                    if (field == IntPtr.Zero) break;
                    if ((FieldGetFlags(field) & LiteralFlag) == 0) continue;
                    string name = Marshal.PtrToStringUTF8(FieldGetName(field)) ?? "";
                    if (string.IsNullOrWhiteSpace(name) || name == "value__") continue;
                    IntPtr boxed = FieldGetValueObject(field, IntPtr.Zero);
                    if (boxed == IntPtr.Zero) continue;
                    IntPtr raw = ObjectUnbox(boxed);
                    if (raw == IntPtr.Zero) continue;
                    values.Add(new RuntimeEnumEntry(Marshal.ReadInt32(raw), name));
                }
                values.Sort((left, right) => {
                    int comparison = left.Id.CompareTo(right.Id);
                    return comparison == 0 ? string.CompareOrdinal(left.Name, right.Name) : comparison;
                });
                if (values.Count > 0) {
                    Cache[typeName] = values;
                    return values;
                }
                Warn(typeName, "No enum literals were available in the loaded image.");
            } catch (Exception ex) {
                Warn(typeName, ex.GetType().Name + ": " + ex.Message);
            }
            return Empty;
        }

        public static string Label(string typeName, int id) {
            foreach (RuntimeEnumEntry entry in Entries(typeName)) {
                if (entry.Id == id) return entry.Name;
            }
            return id.ToString(CultureInfo.InvariantCulture);
        }

        private static void Warn(string typeName, string message) {
            if (Warned.Add(typeName)) {
                NativeSdkServices.Warn("[preview/enums] " + typeName + ": " + message);
            }
        }
    }
}
