#nullable enable
using System;
using UnityEngine;

namespace AstralParty.Modding.SDK.Native {
    /// <summary>
    /// Screen-space hit tests without walking FairyGUI.GObject.parent.
    /// Some HybridCLR wrappers reject that property on a native owner of a
    /// different generated type; hit-testing must not throw in LateUpdate.
    /// </summary>
    public static class NativeUiHitTest {
        public static bool Contains(IntPtr target) {
            if (target == IntPtr.Zero || Screen.width < 1 || Screen.height < 1) return false;
            try {
                if (!NativeGame.IsOnStage(target) || !NativeGame.IsVisible(target)) return false;
                IntPtr root = FairyBridge.Root();
                if (root == IntPtr.Zero) return false;
                float rw = FairyBridge.GetFloat(root, "FairyGUI.GObject", "width");
                float rh = FairyBridge.GetFloat(root, "FairyGUI.GObject", "height");
                Vector3 mouse = Input.mousePosition;
                var hit = FairyBridge.TransformPoint(root,
                    mouse.x * rw / Screen.width,
                    (Screen.height - mouse.y) * rh / Screen.height, target);
                return hit.X >= 0f && hit.Y >= 0f &&
                    hit.X <= FairyBridge.GetFloat(target, "FairyGUI.GObject", "width") &&
                    hit.Y <= FairyBridge.GetFloat(target, "FairyGUI.GObject", "height");
            } catch { return false; } // Window can unload during this frame.
        }

        public static bool ShowingWindow(IntPtr manager, string cacheField) {
            if (manager == IntPtr.Zero) return false;
            try {
                IntPtr window = NativeGame.CachedWindow(manager, cacheField);
                return NativeGame.IsShowing(window) && NativeGame.IsOnStage(NativeGame.WindowPane(window));
            } catch { return false; }
        }
    }
}
