using System;
using IL2CppSharp;

namespace AstralParty.Modding.SDK.Native {
    /// <summary>
    /// Calls the game's own FairyGUI toast window. This does NOT install a Hook.
    /// Source path verified in AstralParty.Runtime decompilation:
    /// Tools.SimpleSingletonProvider&lt;UI.UIManager&gt;._inst
    ///   -> UI.UIManager.systemTips
    ///   -> UI.SystemTipsWindow.ShowTips(string, float)
    /// </summary>
    public static class GameToastService {
        private const string RuntimeAssembly = "AstralParty.Runtime";
        private const string ManagerType = "UI.UIManager";
        private const string ToastType = "UI.SystemTipsWindow";

        private static IntPtr _managerClass;
        private static IntPtr _providerClass;
        private static IntPtr _getSystemTips;
        private static IntPtr _showTips;
        private static bool _hintRegistered;

        public static bool TryShow(string message, float seconds, out string reason) {
            reason = string.Empty;

            try {
                if (!TryResolveMethods(out reason)) {
                    return false;
                }

                // Read the already-existing singleton. Do NOT invoke the lazy
                // .inst getter before the game's UI system has initialized.
                IntPtr manager = Il2CppReflection.GetStaticFieldValue(_providerClass, "_inst");
                if (manager == IntPtr.Zero) {
                    reason = "UIManager singleton has not been created yet.";
                    return false;
                }

                // Resolve a fresh SystemTipsWindow every time: scene transitions
                // may Dispose() and recreate the cached window.
                IntPtr window = Il2CppReflection.InvokeMethod(_getSystemTips, manager, null);
                if (window == IntPtr.Zero) {
                    reason = "UIManager.systemTips returned null; UI may not be ready.";
                    return false;
                }

                float safeSeconds = Math.Clamp(seconds, 0.5f, 8.0f);

                // Exact string/float overload from the actual hot-update DLL.
                // The fluent API handles UTF-16 strings and float marshalling.
                if (!IL2CPP.Method(_showTips).TryInvoke(window, out _, message, safeSeconds)) {
                    reason = "Failed to marshal or invoke ShowTips(string, float).";
                    return false;
                }
                return true;
            } catch (Exception ex) {
                reason = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private static bool TryResolveMethods(out string reason) {
            reason = string.Empty;

            if (!IL2CPP.Initialize()) {
                reason = "IL2CppSharp runtime is not initialized yet.";
                return false;
            }

            if (!_hintRegistered) {
                IL2CPP.AddAssemblyHint(RuntimeAssembly);
                _hintRegistered = true;
            }

            // Try again on later key presses if the hot-update assembly has not
            // loaded yet. Cache metadata pointers only after a full lookup.
            if (_managerClass != IntPtr.Zero && _providerClass != IntPtr.Zero &&
                _getSystemTips != IntPtr.Zero && _showTips != IntPtr.Zero) {
                return true;
            }

            IntPtr managerClass = Il2CppReflection.FindClass(ManagerType);
            IntPtr toastClass = Il2CppReflection.FindClass(ToastType);
            if (managerClass == IntPtr.Zero || toastClass == IntPtr.Zero) {
                reason = "AstralParty.Runtime UI classes are not loaded yet.";
                return false;
            }

            // UIManager : SimpleSingletonProvider<UIManager>.
            // _inst is declared on that constructed generic base type.
            IntPtr providerClass = Il2CppAPI.GetParentClass(managerClass);
            if (providerClass == IntPtr.Zero ||
                Il2CppReflection.FindField(providerClass, "_inst") == IntPtr.Zero) {
                reason = "Could not resolve SimpleSingletonProvider<UIManager>._inst.";
                return false;
            }

            IntPtr getSystemTips = Il2CppReflection.FindMethod(managerClass, "get_systemTips", 0);
            IntPtr showTips = IL2CPP.Class(ToastType)
                .Method("ShowTips")
                .Args<string, float>()
                .Pointer;

            if (getSystemTips == IntPtr.Zero || showTips == IntPtr.Zero) {
                reason = "UIManager.systemTips or ShowTips(string, float) was not found.";
                return false;
            }

            _managerClass = managerClass;
            _providerClass = providerClass;
            _getSystemTips = getSystemTips;
            _showTips = showTips;
            return true;
        }
    }
}
