using AstralParty.Modding.SDK.Native;
using System;
using System.Runtime.InteropServices;
using AstralParty.Modding.SDK;
using AstralParty.Modding.SDK.Native.Interop;

namespace AstralParty.Modding.SDK.Native.UI {
    /// <summary>
    /// Only settings tabs and group headings call this helper. Read the outline
    /// from the Setting prefab's TextFormat (TMP/SDF), not from a body text
    /// field or the legacy GTextField.stroke property alone.
    /// </summary>
    public static class ModCaptionOutline {
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeColor {
            public float R, G, B, A;
        }

        // Inspect the real font type, not the font name: different game builds
        // may register different font implementations under the same alias.
        private static bool UsesTmpFont(IntPtr title) {
            try {
                IntPtr textField = FairyBridge.FieldValue(title, "FairyGUI.GTextField", "_textField");
                if (textField == IntPtr.Zero) return false;
                IntPtr font = FairyBridge.FieldValue(textField, "FairyGUI.TextField", "_font");
                IntPtr tmp = FairyBridge.ClassOrZero("FairyGUI.TMPFont");
                if (font == IntPtr.Zero || tmp == IntPtr.Zero) return false;
                for (IntPtr klass = IL2CppSharp.Il2CppReflection.GetObjectClass(font);
                    klass != IntPtr.Zero; klass = IL2CppSharp.Il2CppAPI.GetParentClass(klass))
                    if (klass == tmp) return true;
            } catch (Exception) { /* Missing optional reflection metadata: conservative fallback. */ }
            return false;
        }

        public static void CopyNative(IntPtr original, IntPtr title) {
            if (original == IntPtr.Zero || title == IntPtr.Zero) return;
            IntPtr source = FairyBridge.GetObject(original, "FairyGUI.GTextField", "textFormat");
            IntPtr target = FairyBridge.GetObject(title, "FairyGUI.GTextField", "textFormat");
            if (source == IntPtr.Zero || target == IntPtr.Zero) return;

            // TextFormat.CopyFrom copies ordinary typography but intentionally
            // omits the TMP effects. Copy their actual native values explicitly.
            float outline = NativeValue.ReadFloat(source, "FairyGUI.TextFormat", "outline");
            float softness = NativeValue.ReadFloat(source, "FairyGUI.TextFormat", "outlineSoftness");
            float dilate = NativeValue.ReadFloat(source, "FairyGUI.TextFormat", "faceDilate");
            // Some prefabs draw their outline in the built-in font material,
            // leaving TextFormat.outline and GTextField.stroke both at zero.
            // Supply a restrained SDF fallback instead of a harsh 3px stroke.
            if (outline <= 0f) {
                outline = FairyBridge.GetFloat(original, "FairyGUI.GTextField", "stroke");
                if (outline <= 0f) {
                    // TMPFont interprets outline as a shader width; DynamicFont
                    // interprets it as a mesh offset in pixels. Mixing these
                    // was why one fallback was invisible or excessively grainy.
                    bool tmp = UsesTmpFont(title);
                    outline = tmp
                        ? Math.Clamp(NativeSdkServices.TitleOutline * 0.08f, 0.18f, 0.3f)
                        : Math.Clamp(NativeSdkServices.TitleOutline * 0.5f, 1.2f, 2f);
                    softness = tmp ? Math.Max(softness, 0.08f) : softness;
                }
            }
            NativeValue.WriteFloat(target, "FairyGUI.TextFormat", "outline", outline);
            NativeValue.WriteFloat(target, "FairyGUI.TextFormat", "outlineSoftness", softness);
            NativeValue.WriteFloat(target, "FairyGUI.TextFormat", "faceDilate", dilate);
            // Always black for MOD titles. Do not copy the prefab's potentially
            // white outline color and never alter the glyph/font pointer.
            Il2CppNative.WriteValue(target, FairyBridge.Field("FairyGUI.TextFormat", "outlineColor"),
                new NativeColor { R = 0f, G = 0f, B = 0f, A = 1f });
            NativeReferenceFields.Call(title, "set_textFormat", target);
            // GTextField.stroke uses the SAME TextFormat.outline, but its
            // setter explicitly marks the text mesh dirty after a prefab swap.
            FairyBridge.SetTextOutline(title, outline, 0x000000);
            if (NativeSdkServices.UiTypographyDiagnostics)
                NativeSdkServices.Info("MOD native caption SDF: outline=" + outline + " softness=" + softness);
        }
    }
}
