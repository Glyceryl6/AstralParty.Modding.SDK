using AstralParty.Modding.SDK.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

using AstralParty.Modding.SDK.Native.UI;

namespace AstralParty.Modding.SDK.Native.Interop {
    public static class InspectorEnumLabels {
        private static readonly Dictionary<IntPtr, string> Labels = new();
        private static readonly Dictionary<string, string> FileLabels = new(StringComparer.Ordinal);
        private static readonly HashSet<string> Warned = new(StringComparer.Ordinal);
        private static bool _exportsUnavailable;
        private static string _fileStamp = "";
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_custom_attrs_from_field")]
        private static extern IntPtr FromField(IntPtr field);
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_custom_attrs_get_attr")]
        private static extern IntPtr GetAttribute(IntPtr info, IntPtr klass);
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_custom_attrs_free")]
        private static extern void Free(IntPtr info);
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_string_length")]
        private static extern int StringLength(IntPtr text);
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_string_chars")]
        private static extern IntPtr StringChars(IntPtr text);

        public static string Label(string enumName, string symbol) {
            string native = ReadNative(enumName, symbol);
            if (!string.IsNullOrWhiteSpace(native)) return native;
            RefreshFile();
            return FileLabels.GetValueOrDefault(enumName + "." + symbol, symbol);
        }
        
        private static string ReadNative(string enumName, string symbol) {
            if (_exportsUnavailable) return "";
            try {
                IntPtr attributeClass = FairyBridge.ClassOrZero("UnityEngine.InspectorNameAttribute");
                if (attributeClass == IntPtr.Zero) return "";
                IntPtr field = FairyBridge.Field(enumName, symbol);
                if (Labels.TryGetValue(field, out string? cached)) return cached;
                IntPtr info = FromField(field); // Unity export takes FieldInfo*, not (class, field).
                if (info == IntPtr.Zero) return "";
                try {
                    IntPtr attribute = GetAttribute(info, attributeClass);
                    if (attribute == IntPtr.Zero) return "";
                    using var lease = new NativeObjectLease(attribute);
                    // Unity defines public readonly string displayName: it is a FIELD, not a getter.
                    IntPtr text = FairyBridge.FieldValue(attribute, "UnityEngine.InspectorNameAttribute", "displayName");
                    if (text == IntPtr.Zero) return "";
                    int length = StringLength(text);
                    if (length < 1 || length > 8192) return "";
                    string label = Marshal.PtrToStringUni(StringChars(text), length) ?? "";
                    if (!string.IsNullOrWhiteSpace(label)) Labels[field] = label;
                    return label;
                } finally { Free(info); }
            } catch (EntryPointNotFoundException) { _exportsUnavailable = true; Warn("exports", "Native field-attribute exports unavailable; using original DLL metadata when configured."); }
            catch (Exception ex) { Warn(enumName, ex.Message); }
            return "";
        }
        
        private static void RefreshFile() {
            string path = NativeSdkServices.EnumLabelAssemblyPath.Trim();
            if (string.IsNullOrEmpty(path)) { FileLabels.Clear(); _fileStamp = ""; return; }
            try {
                path = Path.GetFullPath(path);
                var file = new FileInfo(path);
                string stamp = path + "|" + file.LastWriteTimeUtc.Ticks + "|" + file.Length;
                if (_fileStamp == stamp) return;
                var labels = new Dictionary<string, string>(StringComparer.Ordinal);
                using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var pe = new PEReader(stream);
                if (!pe.HasMetadata) throw new BadImageFormatException("The selected file has no managed metadata");
                MetadataReader metadata = pe.GetMetadataReader();
                foreach (TypeDefinitionHandle typeHandle in metadata.TypeDefinitions) {
                    TypeDefinition type = metadata.GetTypeDefinition(typeHandle);
                    string name = metadata.GetString(type.Name);
                    if (name != "ConditionType" && name != "MissionConditionType") continue;
                    foreach (FieldDefinitionHandle fieldHandle in type.GetFields()) {
                        FieldDefinition field = metadata.GetFieldDefinition(fieldHandle);
                        foreach (CustomAttributeHandle attrHandle in field.GetCustomAttributes()) {
                            CustomAttribute attr = metadata.GetCustomAttribute(attrHandle);
                            EntityHandle owner;
                            if (attr.Constructor.Kind == HandleKind.MemberReference)
                                owner = metadata.GetMemberReference((MemberReferenceHandle)attr.Constructor).Parent;
                            else if (attr.Constructor.Kind == HandleKind.MethodDefinition)
                                owner = metadata.GetMethodDefinition((MethodDefinitionHandle)attr.Constructor).GetDeclaringType();
                            else continue;
                            string attrName, attrNamespace;
                            if (owner.Kind == HandleKind.TypeReference) {
                                TypeReference t = metadata.GetTypeReference((TypeReferenceHandle)owner);
                                attrName = metadata.GetString(t.Name); attrNamespace = metadata.GetString(t.Namespace);
                            } else if (owner.Kind == HandleKind.TypeDefinition) {
                                TypeDefinition t = metadata.GetTypeDefinition((TypeDefinitionHandle)owner);
                                attrName = metadata.GetString(t.Name); attrNamespace = metadata.GetString(t.Namespace);
                            } else continue;
                            if (attrName != "InspectorNameAttribute" || attrNamespace != "UnityEngine") continue;
                            BlobReader blob = metadata.GetBlobReader(attr.Value);
                            if (blob.ReadUInt16() != 1) continue;
                            string? label = blob.ReadSerializedString();
                            if (!string.IsNullOrWhiteSpace(label)) labels[name + "." + metadata.GetString(field.Name)] = label;
                        }
                    }
                }
                FileLabels.Clear(); foreach (var pair in labels) FileLabels.Add(pair.Key, pair.Value);
                _fileStamp = stamp;
            } catch (Exception ex) { FileLabels.Clear(); _fileStamp = ""; Warn(path, ex.Message); }
        }
        
        private static void Warn(string source, string message) {
            if (Warned.Add(source)) NativeSdkServices.Warn("[stats/enum-labels] " + source + ": " + message);
        }
    }
}
