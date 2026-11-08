using System;
using System.Runtime.InteropServices;
using IL2CppSharp;
using AstralParty.Modding.SDK;


namespace AstralParty.Modding.SDK.Native {
    /// <summary>
    /// A text-only native button shared by extension-page actions and the
    /// battle route confirmation. The old Common/Button_Next art contains
    /// left/right arrow glyphs and is not suitable for generic actions.
    /// 公共文字按钮：不用带左右箭头的 Button_Next 预制体。
    /// </summary>
    public static class NativeActionButton {
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "il2cpp_field_set_value")]
        private static extern void SetNativeByte(IntPtr instance, IntPtr field, ref byte value);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeColor {
            public float R, G, B, A;
            public NativeColor(float r, float g, float b, float a = 1f) {
                R = r; G = g; B = b; A = a;
            }
        }

        /// <summary>创建带文字的游戏原生按钮；必须先确认对应资源包已加载，调用者负责对象生命周期。</summary>
        public static IntPtr Create(IntPtr parent, string caption, float width,
            float height, out IntPtr text) {
            // Home is routinely unloaded during battle, and the settings package
            // may also be absent. Common/Button_Next2 is referenced by the game's
            // generated UI and remains a usable native action button in battle.
            // 对局内 Home / Setting 资源可能已卸载；优先用文字按钮，最后回退
            // 到原版 Common 操作按钮，避免反复报 prefab missing。
            // Prefer the colored native Settings tab/button. Home's mostly
            // white Hero_Button_Set was invisible with a white label in Replay.
            // 回放管理按钮优先彩色原生控件，避免白底配白字。
            IntPtr button = TryPrefab(NativeUiCatalog.ActionButton.Package,
                NativeUiCatalog.ActionButton.Item);
            if (button == IntPtr.Zero) {
                button = TryPrefab("Home", "Hero_Button_Set");
            }
            if (button == IntPtr.Zero) {
                button = TryPrefab("Common", "Button_Next2");
            }
            if (button == IntPtr.Zero) {
                button = TryPrefab("Common", "Button_Next");
            }
            if (button == IntPtr.Zero) {
                throw new InvalidOperationException(
                    "Action button resources are unavailable (Home/Setting/Common); wait for UI packages to load");
            }
            text = IntPtr.Zero;
            try {
                FairyBridge.SetInt(button, "FairyGUI.GButton", "mode",
                    (int)NativeButtonMode.Check);
                IntPtr field = FairyBridge.Field("FairyGUI.GButton", "changeStateOnClick");
                byte click = 1;
                SetNativeByte(button, field, ref click);
                FairyBridge.SetBool(button, "FairyGUI.GButton", "selected", false);
                NativeToggleControl.EnsureFullRectHitArea(button);
                FairyBridge.SetString(button, "FairyGUI.GButton", "title", "");
                FairyBridge.SetString(button, "FairyGUI.GButton", "selectedTitle", "");
                // Resizing the GButton root alone does not necessarily resize its
                // 9-sliced child artwork. A 540-unit caption can otherwise sit far
                // right of the original 150-unit border (observed in Replay Tools).
                // 仅放大 GButton 根节点并不会使原版底图随之变宽。
                StretchNativeArtwork(button, width, height);
                FairyBridge.AddChild(parent, button);
    
                // Independent caption is not driven by the button's stock
                // selected-state GearXY / transition. / 文字不受状态齿轮移动影响。
                float inset = NativeSdkServices.ActionButtonTextInset;
                text = FairyBridge.AddText(button, NativeSdkServices.Translate(caption),
                    NativeSdkServices.ActionButtonFontSize, 0x202020, inset, 0f,
                    Math.Max(20f, width - 2f * inset), height);
                FairyBridge.CenterText(text);
                FairyBridge.SetBool(text, "FairyGUI.GObject", "touchable", false);
                FairyBridge.SetInt(text, "FairyGUI.GTextField", "autoSize", 0);
                StableNativeAction.Prepare(button, text);
                return button;
            } catch (Exception) {
                DisposeButton(button);
                throw;
            }
        }


        /// <summary>
        /// A standard rectangular action button without the tab prefab's
        /// decorative circle. The opaque pink background stays above native
        /// artwork through check-state transitions, but below the caption.
        /// 归档/恢复按钮使用普通粉色矩形，不再带选项卡的装饰圆点。
        /// 创建不带原版方向箭头的文字按钮，适合 MOD 菜单与扩展操作列表。
        /// </summary>
        public static IntPtr CreatePlain(IntPtr parent, string caption, float width,
            float height, out IntPtr text) {
            IntPtr button = Create(parent, caption, width, height, out text);
            try {
                IntPtr graph = Il2CppAPI.CreateObject(FairyBridge.Class("FairyGUI.GGraph"));
                Il2CppReflection.InvokeMethod(FairyBridge.Method("FairyGUI.GGraph", ".ctor", 0),
                    graph, Array.Empty<IntPtr>());
                IntPtr color = FairyBridge.Class("UnityEngine.Color");
                IntPtr[] args = {
                    Il2CppReflection.BoxValue(FairyBridge.Class("System.Single"), width),
                    Il2CppReflection.BoxValue(FairyBridge.Class("System.Single"), height),
                    Il2CppReflection.BoxValue(FairyBridge.Class("System.Int32"), 2),
                    Il2CppReflection.BoxValue(color,
                        new NativeColor(0.87f, 0.10f, 0.45f)),
                    Il2CppReflection.BoxValue(color,
                        new NativeColor(1f, 0.13f, 0.54f))
                };
                Il2CppReflection.InvokeMethod(FairyBridge.Method("FairyGUI.GGraph", "DrawRect", 5),
                    graph, args);
                FairyBridge.SetBool(graph, "FairyGUI.GObject", "touchable", false);
                FairyBridge.AddChild(button, graph);
                // Graph is rendered after the prefab circle but before our text.
                Il2CppReflection.InvokeMethod(FairyBridge.Method("FairyGUI.GComponent",
                    "SetChildIndex", 2), button, new[] {
                        graph, Il2CppReflection.BoxValue(FairyBridge.Class("System.Int32"),
                            FairyBridge.ChildCount(button) - 2)
                    });
                NativeToggleControl.EnsureFullRectHitArea(button);
                return button;
            } catch (Exception) {
                DisposeButton(button);
                throw;
            }
        }

        /// <summary>
        /// Stretch only large backing shapes of the native button. Do not
        /// stretch icons or small arrow decorations. The original game uses
        /// nine-sliced images, while cloning SetSize(root) alone is insufficient.
        /// 只延展覆盖按钮大部分面积的底图，不拉伸箭头或小图标。
        /// 仅调整已创建按钮的原生底图尺寸；不负责设置父级布局或注册事件。
        /// </summary>
        public static void StretchNativeArtwork(IntPtr button, float width, float height) {
            float sourceWidth = FairyBridge.GetFloat(button, "FairyGUI.GObject", "width");
            float sourceHeight = FairyBridge.GetFloat(button, "FairyGUI.GObject", "height");
            if (sourceWidth < 1f || sourceHeight < 1f) {
                FairyBridge.SetSize(button, width, height);
                return;
            }
            float deltaWidth = width - sourceWidth;
            float deltaHeight = height - sourceHeight;
            int count = FairyBridge.ChildCount(button);
            for (int i = 0; i < count; i++) {
                IntPtr child = FairyBridge.GetChildAt(button, i);
                if (child == IntPtr.Zero || NativeGame.IsInstanceOf(child, "FairyGUI.GTextField") || NativeGame.IsInstanceOf(child, "FairyGUI.GGroup")) {
                    continue;
                }
                float childWidth = FairyBridge.GetFloat(child, "FairyGUI.GObject", "width");
                float childHeight = FairyBridge.GetFloat(child, "FairyGUI.GObject", "height");
                if (childWidth < sourceWidth * 0.65f ||
                    childHeight < sourceHeight * 0.50f) {
                    continue;
                }
                // Native controller relations may otherwise restore the old size.
                // 克隆按钮的图片仍保留原材质/九宫格资源，只调整几何宽高。
                FairyBridge.SetSize(child, Math.Max(32f, childWidth + deltaWidth),
                    Math.Max(20f, childHeight + deltaHeight));
            }
            FairyBridge.SetSize(button, width, height);
        }

        private static void DisposeButton(IntPtr button) {
            if (button == IntPtr.Zero) {
                return;
            }
            try {
                Il2CppReflection.InvokeMethod(FairyBridge.Method("FairyGUI.GComponent",
                    "Dispose", 0), button, Array.Empty<IntPtr>());
            } catch (Exception) {
                // Preserve the original construction failure, not cleanup error.
            }
        }

        private static IntPtr TryPrefab(string package, string item) {
            try {
                return FairyBridge.PackageItem(package, item);
            } catch (Exception) {
                // A missing UI package is normal during scene transitions.
                // 游戏切场景时缺少包是正常情况；调用方应选择其它资源。
                return IntPtr.Zero;
            }
        }

        /// <summary>更新按钮上的原生文字，需在 Unity 主线程执行。</summary>
        public static void UpdateText(IntPtr text, string value) {
            if (text != IntPtr.Zero) {
                FairyBridge.SetString(text, "FairyGUI.GTextField", "text", value);
            }
        }
    }
}
