using AstralParty.Modding.SDK;
using System;
using System.Runtime.InteropServices;
using IL2CppSharp;
using AstralParty.Modding.SDK.Native.Interop;

namespace AstralParty.Modding.SDK.Native.UI {
    /// <summary>
    /// Creates actual FairyGUI.GTextField objects without the title prefab's
    /// oversized background/font. No reference to AstralParty.Runtime is needed.
    /// All calls must execute on the Unity main thread.
    /// </summary>
    public static class NativeFairyText {
        public static IntPtr Create(string value, int fontSize, uint rgb) {
            IntPtr klass = RequireClass("FairyGUI.GTextField");
            IntPtr constructor = RequireMethod(klass, ".ctor", 0);
            IntPtr text = Il2CppAPI.CreateObject(klass);
            if (text == IntPtr.Zero) {
                throw new InvalidOperationException("Cannot allocate FairyGUI.GTextField");
            }
            Il2CppReflection.InvokeMethod(constructor, text, Array.Empty<IntPtr>());
            ApplyStyle(text, fontSize, rgb);
            IntPtr setText = RequireMethod(klass, "set_text", 1);
            Il2CppReflection.InvokeMethodWithString(setText, text, value);
            return text;
        }

        public static void ApplyStyle(IntPtr text, int fontSize, uint rgb) {
            IntPtr textClass = RequireClass("FairyGUI.GTextField");
            IntPtr format = Il2CppReflection.InvokeMethod(
                RequireMethod(textClass, "get_textFormat", 0), text, Array.Empty<IntPtr>());
            if (format == IntPtr.Zero) {
                throw new InvalidOperationException("FairyGUI.TextFormat is unavailable");
            }
            IntPtr formatClass = RequireClass("FairyGUI.TextFormat");
            IntPtr sizeField = Il2CppReflection.FindField(formatClass, "size");
            if (sizeField == IntPtr.Zero) {
                throw new MissingFieldException("FairyGUI.TextFormat.size");
            }
            int size = Math.Clamp(fontSize, 12, 64);
            Il2CppNative.WriteValue(format, sizeField, size);
            Il2CppReflection.InvokeMethodWithInt(
                RequireMethod(formatClass, "SetColor", 1), format, unchecked((int)rgb));
            // GTextField does not expose ApplyFormat() (that method belongs
            // to FairyGUI.TextField). Re-assigning its TextFormat is the
            // supported way to apply size/color on GTextField.
            Il2CppReflection.InvokeMethod(
                RequireMethod(textClass, "set_textFormat", 1), text, new[] { format });
        }

        /// <summary>
        /// Copy the FORMAT from a known Chinese-capable native checkbox label,
        /// then change only the numeric point size. We intentionally never
        /// write IL2CPP string fields or call set_textFormat with an injected
        /// font pointer: the v0.16.0 crash occurred precisely in that path.
        /// 从原版开关正文复制已存在的中文文本格式。绝不写入字体字符串原生指针，
        /// v0.16.0 的原生 AccessViolation 就发生在那条路径中。
        /// </summary>
        public static void CopyTitleFormat(IntPtr source, IntPtr target, int size) {
            IntPtr original = FairyBridge.GetObject(source, "FairyGUI.GTextField", "textFormat");
            IntPtr own = FairyBridge.GetObject(target, "FairyGUI.GTextField", "textFormat");
            if (original == IntPtr.Zero || own == IntPtr.Zero)
                throw new InvalidOperationException("Native text format missing");
            Il2CppReflection.InvokeMethod(FairyBridge.Method("FairyGUI.TextFormat", "CopyFrom", 1),
                own, new[] { original });
            Il2CppReflection.InvokeMethod(
                FairyBridge.Method("FairyGUI.GTextField", "set_textFormat", 1),
                target, new[] { own });
            ApplyStyle(target, size, 0xFFFFFF);
        }

        /// <summary>
        /// Copy a genuine native caption's outline without turning it black or
        /// resetting it. Useful for non-settings UI such as monster movement
        /// counters, which should look identical to their native counterpart.
        /// </summary>
        public static void CopyNativeOutline(IntPtr source, IntPtr target) {
            if (source == IntPtr.Zero || target == IntPtr.Zero) return;
            IntPtr original = FairyBridge.GetObject(source, "FairyGUI.GTextField", "textFormat");
            IntPtr own = FairyBridge.GetObject(target, "FairyGUI.GTextField", "textFormat");
            if (original == IntPtr.Zero || own == IntPtr.Zero) return;
            foreach (string property in new[] { "outline", "outlineSoftness", "faceDilate", "underlaySoftness" })
                NativeValue.WriteFloat(own, "FairyGUI.TextFormat", property,
                    NativeValue.ReadFloat(original, "FairyGUI.TextFormat", property));
            NativeReferenceFields.Call(target, "set_textFormat", own);
            // The native color is a UnityEngine.Color value type, not a managed
            // reference: use the typed getter/setter rather than a raw pointer.
            var color = CheckedNativeCall.Get<UnityEngine.Color>(source,
                "FairyGUI.GTextField", "strokeColor");
            CheckedNativeCall.Set(target, "FairyGUI.GTextField", "strokeColor", color);
            FairyBridge.SetFloat(target, "FairyGUI.GTextField", "stroke",
                FairyBridge.GetFloat(source, "FairyGUI.GTextField", "stroke"));
        }

        /// <summary>Action buttons must never inherit a white prefab outline.</summary>
        public static void ClearOutline(IntPtr text) {
            if (text == IntPtr.Zero) return;
            IntPtr format = FairyBridge.GetObject(text, "FairyGUI.GTextField", "textFormat");
            if (format == IntPtr.Zero) return;
            NativeValue.WriteFloat(format, "FairyGUI.TextFormat", "outline", 0f);
            // Set the format and clear legacy stroke; selected-state changes
            // must not restore the old white halo on the button's caption.
            NativeReferenceFields.Call(text, "set_textFormat", format);
            FairyBridge.SetFloat(text, "FairyGUI.GTextField", "stroke", 0f);
        }

        private static IntPtr RequireClass(string typeName) {
            // Share FairyBridge's positive class cache and parent-chain fallback.
            // 共享类缓存，避免不同 UI 模块重复使用不同的类型解析策略。
            return FairyBridge.Class(typeName);
        }

        private static IntPtr RequireMethod(IntPtr klass, string name, int arguments) {
            IntPtr method = Il2CppReflection.FindMethodInHierarchy(klass, name, arguments);
            if (method == IntPtr.Zero) {
                throw new MissingMethodException(name + "(" + arguments + ")");
            }
            return method;
        }
    }
}
