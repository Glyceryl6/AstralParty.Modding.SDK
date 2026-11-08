using AstralParty.Modding.SDK.Native;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AstralParty.Modding.SDK;
using IL2CppSharp;

using AstralParty.Modding.SDK.Native.Interop;

namespace AstralParty.Modding.SDK.Native.UI {
    /// <summary>
    /// Minimal runtime-only FairyGUI bridge. Game hot-update DLL is NOT a build reference.
    /// All methods must be called from Unity's main thread.
    /// </summary>
    public static class FairyBridge {
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeColor {
            public float R;
            public float G;
            public float B;
            public float A;
        }

        private static readonly Dictionary<string, IntPtr> Classes = new Dictionary<string, IntPtr>();
        private static readonly Dictionary<string, IntPtr> Methods = new Dictionary<string, IntPtr>();
        private static readonly Dictionary<string, IntPtr> Fields = new Dictionary<string, IntPtr>();
        private static bool _hintAdded;

        public static bool Ready() {
            if (!IL2CPP.Initialize()) {
                return false;
            }
            if (!_hintAdded) {
                IL2CPP.AddAssemblyHint("AstralParty.Runtime");
                _hintAdded = true;
            }
            // A GRoot pointer alone is not enough: native UI operations also
            // need the GObject/GComponent bases (possibly resolved via hierarchy).
            // 避免在 HybridCLR 类型只加载了一部分时创建 UI。
            return ClassOrZero("FairyGUI.GRoot") != IntPtr.Zero &&
                ClassOrZero("FairyGUI.GComponent") != IntPtr.Zero &&
                ClassOrZero("FairyGUI.GObject") != IntPtr.Zero;
        }

        public static IntPtr ClassOrZero(string type) {
            if (Classes.TryGetValue(type, out IntPtr klass) && klass != IntPtr.Zero) {
                return klass;
            }
            klass = Il2CppReflection.FindClass(type);
            // If HybridCLR's image lookup misses a base type, recover it from
            // the known hierarchy GRoot : GComponent : GObject in game sources.
            // 若镜像查找漏掉 FairyGUI 基类，从 GRoot 父类链取回指针。
            // No extra P/Invoke metadata API is needed for this fallback.
            if (klass == IntPtr.Zero &&
                (type == "FairyGUI.GComponent" || type == "FairyGUI.GObject")) {
                IntPtr rootClass = Il2CppReflection.FindClass("FairyGUI.GRoot");
                if (rootClass != IntPtr.Zero) {
                    klass = Il2CppAPI.GetParentClass(rootClass);
                    if (type == "FairyGUI.GObject" && klass != IntPtr.Zero) {
                        klass = Il2CppAPI.GetParentClass(klass);
                    }
                }
            }
            if (klass != IntPtr.Zero) {
                Classes[type] = klass;
            }
            return klass;
        }

        public static IntPtr Class(string type) {
            IntPtr klass = ClassOrZero(type);
            if (klass == IntPtr.Zero) {
                throw new TypeLoadException(type);
            }
            return klass;
        }

        public static IntPtr Method(string type, string name, int arity) {
            string key = type + ":" + name + "/" + arity;
            if (Methods.TryGetValue(key, out IntPtr method) && method != IntPtr.Zero) {
                return method;
            }
            method = Il2CppReflection.FindMethodInHierarchy(Class(type), name, arity);
            if (method == IntPtr.Zero) {
                throw new MissingMethodException(type, name + "(" + arity + ")");
            }
            Methods[key] = method;
            return method;
        }

        public static IntPtr TryMethod(string type, string name, int arity) {
            IntPtr klass = ClassOrZero(type);
            return klass == IntPtr.Zero ? IntPtr.Zero : Il2CppReflection.FindMethodInHierarchy(klass, name, arity);
        }

        public static IntPtr Field(string type, string name) {
            string key = type + ":" + name;
            if (Fields.TryGetValue(key, out IntPtr field) && field != IntPtr.Zero) {
                return field;
            }
            field = Il2CppReflection.FindField(Class(type), name);
            if (field == IntPtr.Zero) {
                throw new MissingFieldException(type, name);
            }
            Fields[key] = field;
            return field;
        }

        public static IntPtr FieldValue(IntPtr instance, string type, string field) {
            return instance == IntPtr.Zero ? IntPtr.Zero :
                Il2CppReflection.GetFieldValue(instance, Field(type, field));
        }

        public static IntPtr GetObject(IntPtr instance, string type, string name) {
            return instance == IntPtr.Zero ? IntPtr.Zero :
                CheckedNativeCall.GetObject(instance, type, name);
        }

        public static bool GetBool(IntPtr instance, string type, string name) {
            return instance != IntPtr.Zero &&
                CheckedNativeCall.Get<bool>(instance, type, name);
        }

        public static int GetInt(IntPtr instance, string type, string name) {
            return CheckedNativeCall.Get<int>(instance, type, name);
        }

        public static float GetFloat(IntPtr instance, string type, string name) {
            return CheckedNativeCall.Get<float>(instance, type, name);
        }

        public static void SetBool(IntPtr instance, string type, string name, bool value) {
            CheckedNativeCall.Set(instance, type, name, value);
        }

        public static void SetInt(IntPtr instance, string type, string name, int value) {
            CheckedNativeCall.Set(instance, type, name, value);
        }

        /// <summary>Setter for native float properties / 安全的原生 float 属性调用。</summary>
        public static void SetFloat(IntPtr instance, string type, string name, float value) {
            CheckedNativeCall.Set(instance, type, name, value);
        }

        /// <summary>
        /// FairyGUI GTextField outline uses stroke + strokeColor, NOT TextFormat.font.
        /// 标题描边只写 float 与 Unity Color，不接触曾导致崩溃的字体字符串指针。
        /// </summary>
        public static void SetTextOutline(IntPtr text, float stroke, uint rgb = 0x101010) {
            SetFloat(text, "FairyGUI.GTextField", "stroke", stroke);
            SetColor(text, "FairyGUI.GTextField", "strokeColor", rgb);
        }

        /// <summary>Read a managed string returned by a native FairyGUI getter.
        /// 读取原生组件 getter 返回的字符串，避免直接读 IL2CPP 字符串指针。</summary>
        public static string GetString(IntPtr instance, string type, string name) {
            return CheckedNativeCall.GetString(instance, type, name);
        }

        public static void SetString(IntPtr instance, string type, string name, string value) {
            if (name == "text" || name == "title" || name == "selectedTitle")
                value = NativeSdkServices.Translate(value);
            CheckedNativeCall.SetString(instance, type, name, value);
        }

        /// <summary>
        /// FairyGUI.GObject.name is a public IL2CPP field, not a property.
        /// It has no set_name method, so use a checked reference-field write.
        /// </summary>
        public static void SetObjectName(IntPtr instance, string name) {
            if (instance == IntPtr.Zero) throw new ArgumentNullException(nameof(instance));
            IntPtr nativeName = Il2CppNative.NewString(name ?? "");
            using var nameLease = new NativeObjectLease(nativeName);
            NativeReferenceFields.Set(instance, "FairyGUI.GObject", "name", nativeName);
        }

        public static void SetXY(IntPtr instance, float x, float y) {
            Il2CppReflection.InvokeMethodWith2Floats(Method("FairyGUI.GObject", "SetXY", 2), instance, x, y);
        }

        public static void SetSize(IntPtr instance, float w, float h) {
            Il2CppReflection.InvokeMethodWith2Floats(Method("FairyGUI.GObject", "SetSize", 2), instance, w, h);
        }


        /// <summary>
        /// Converts a point from a nested FairyGUI object's local space into
        /// the coordinate space of another component. GList scroll offsets,
        /// pivots and panel scaling must not be approximated by adding x/y.
        /// </summary>
        public static FairyPoint TransformPoint(IntPtr source, float x, float y, IntPtr target) {
            if (source == IntPtr.Zero || target == IntPtr.Zero) {
                throw new ArgumentException("TransformPoint requires two valid FairyGUI objects");
            }
            IntPtr vectorClass = Class("UnityEngine.Vector2");
            IntPtr boxed = Il2CppReflection.BoxValue(vectorClass, new FairyPoint { X = x, Y = y });
            if (boxed == IntPtr.Zero) {
                throw new InvalidOperationException("Unable to box UnityEngine.Vector2");
            }
            return Il2CppReflection.InvokeGet<FairyPoint>(
                Method("FairyGUI.GObject", "TransformPoint", 2), source,
                new[] { boxed, target });
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FairyPoint {
            public float X;
            public float Y;
        }

        /// <summary>
        /// Align a dynamically created text field, without depending on
        /// the game hot-update source or the generated FairyGUI wrappers.
        /// </summary>
        public static void CenterText(IntPtr text) {
            SetInt(text, "FairyGUI.GTextField", "align", 1);
            SetInt(text, "FairyGUI.GTextField", "verticalAlign", 1);
            SetBool(text, "FairyGUI.GTextField", "singleLine", true);
        }

        public static void AddChild(IntPtr parent, IntPtr child) {
            if (parent == IntPtr.Zero || child == IntPtr.Zero) {
                throw new ArgumentException("FairyGUI.AddChild cannot accept a null object");
            }
            IntPtr result = Il2CppReflection.InvokeMethod(
                Method("FairyGUI.GComponent", "AddChild", 1), parent, new[] { child });
            if (result == IntPtr.Zero) {
                throw new InvalidOperationException("FairyGUI.AddChild failed");
            }
        }

        public static IntPtr InvokeMethodWithInt(string type, string method,
            IntPtr instance, int value) {
            return Il2CppReflection.InvokeMethodWithInt(Method(type, method, 1), instance, value);
        }

        public static IntPtr GetChildAt(IntPtr parent, int index) {
            return Il2CppReflection.InvokeMethodWithInt(
                Method("FairyGUI.GComponent", "GetChildAt", 1), parent, index);
        }

        public static int ChildCount(IntPtr parent) {
            return GetInt(parent, "FairyGUI.GComponent", "numChildren");
        }

        public static IntPtr Root() {
            return IL2CPP.Class("FairyGUI.GRoot").Method("get_inst").Args().InvokeStatic();
        }

        public static IntPtr PackageItem(string package, string item) {
            return IL2CPP.Class("FairyGUI.UIPackage").Method("CreateObject")
                .Args<string, string>().InvokeStatic(package, item);
        }

        public static IntPtr NewComponent() {
            IntPtr klass = Class("FairyGUI.GComponent");
            IntPtr instance = Il2CppAPI.CreateObject(klass);
            if (instance == IntPtr.Zero) {
                throw new InvalidOperationException("Could not allocate FairyGUI.GComponent");
            }
            Il2CppReflection.InvokeMethod(Method("FairyGUI.GComponent", ".ctor", 0),
                instance, Array.Empty<IntPtr>());
            return instance;
        }

        public static void SetColor(IntPtr instance, string type, string property, uint rgb) {
            IntPtr colorClass = Class("UnityEngine.Color");
            NativeColor color = new NativeColor {
                R = ((rgb >> 16) & 255) / 255f,
                G = ((rgb >> 8) & 255) / 255f,
                B = (rgb & 255) / 255f,
                A = 1f
            };
            IntPtr boxed = Il2CppReflection.BoxValue(colorClass, color);
            if (boxed == IntPtr.Zero) {
                throw new InvalidOperationException("Unable to box native UnityEngine.Color");
            }
            Il2CppReflection.InvokeMethod(Method(type, "set_" + property, 1), instance, new[] { boxed });
        }

        public static IntPtr CreateText(string text, int size, uint color) {
            return NativeFairyText.Create(NativeSdkServices.Translate(text), size, color);
        }

        public static IntPtr AddText(IntPtr parent, string text, int size, uint color,
            float x, float y, float width, float height) {
            IntPtr result = CreateText(text, size, color);
            SetSize(result, width, height);
            SetXY(result, x, y);
            AddChild(parent, result);
            return result;
        }
    }
}
