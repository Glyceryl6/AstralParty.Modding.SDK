using System;
using IL2CppSharp;

namespace AstralParty.Modding.SDK.Native {
    /// <summary>
    /// Read-only probe for transient game tips, used by delayed screenshots.
    /// Never creates or alters the original game's settings window.
    /// </summary>
    public static class GameUiState {
        private static bool _hintAdded;
        private static IntPtr _managerClass;
        private static IntPtr _providerClass;
        private static IntPtr _tipClass;
        private static IntPtr _tipsField;
        private static IntPtr _isShowing;
        private static IntPtr _getContentPane;
        private static IntPtr _showMessageField;
        private static IntPtr _controllerIndexGetter;
        private static IntPtr _gameTipsField;
        private static IntPtr _getGameTipsPane;
        private static IntPtr _gameTipsClass;
        private static IntPtr _generatedTipsPaneClass;
        private static IntPtr _getGObjectVisible;
        private static readonly string[] TransientTipFields = {
            "common", "roundReward", "com_Thinking", "passiveSkillTrigger",
            "com_RoundStart", "levelUp", "yourRoundStart", "changeActionOrder",
            "lotteryRound", "com_PveTaskTip", "com_ChosenOne", "com_PveClueTaskTip"
        };


        private static bool TryGetManager(out IntPtr manager) {
            manager = IntPtr.Zero;
            if (!IL2CPP.Initialize()) {
                return false;
            }
            if (!_hintAdded) {
                IL2CPP.AddAssemblyHint("AstralParty.Runtime");
                _hintAdded = true;
            }
            if (_managerClass == IntPtr.Zero) {
                _managerClass = Il2CppReflection.FindClass("UI.UIManager");
                if (_managerClass == IntPtr.Zero) {
                    return false;
                }
                _providerClass = Il2CppAPI.GetParentClass(_managerClass);
            }
            if (_providerClass == IntPtr.Zero) {
                return false;
            }
            manager = Il2CppReflection.GetStaticFieldValue(_providerClass, "_inst");
            return manager != IntPtr.Zero;
        }

        public static bool TryIsAnyTransientTipVisible(out bool visible) {
            visible = false;
            bool systemKnown = TryIsGameToastVisible(out bool systemVisible);
            if (systemKnown && systemVisible) {
                visible = true;
                return true;
            }
            bool tipsKnown = TryIsGameTipsWindowVisible(out bool tipsVisible);
            visible = tipsVisible;
            return systemKnown || tipsKnown;
        }

        // TipsWindow.ShowTips(content, duration) sets win.common.visible=true,
        // then sets it to false after the duration. Other transient animations
        // also change the visible state of their own FairyGUI components.
        private static bool TryIsGameTipsWindowVisible(out bool visible) {
            visible = false;
            if (!TryGetManager(out IntPtr manager)) {
                return false;
            }
            if (_gameTipsField == IntPtr.Zero) {
                _gameTipsField = Il2CppReflection.FindField(_managerClass, "_cachedTipsWindow");
            }
            if (_gameTipsClass == IntPtr.Zero) {
                _gameTipsClass = Il2CppReflection.FindClass("UI.TipsWindow");
                if (_gameTipsClass != IntPtr.Zero) {
                    _getGameTipsPane = Il2CppReflection.FindMethodInHierarchy(
                        _gameTipsClass, "get_contentPane", 0);
                }
            }
            if (_generatedTipsPaneClass == IntPtr.Zero) {
                _generatedTipsPaneClass = Il2CppReflection.FindClass("UI.UITipsWindow");
            }
            if (_getGObjectVisible == IntPtr.Zero) {
                IntPtr gObjectClass = Il2CppReflection.FindClass("FairyGUI.GObject");
                if (gObjectClass != IntPtr.Zero) {
                    _getGObjectVisible = Il2CppReflection.FindMethodInHierarchy(
                        gObjectClass, "get_visible", 0);
                }
            }
            if (_gameTipsField == IntPtr.Zero || _gameTipsClass == IntPtr.Zero ||
                _generatedTipsPaneClass == IntPtr.Zero || _getGameTipsPane == IntPtr.Zero ||
                _getGObjectVisible == IntPtr.Zero || _isShowing == IntPtr.Zero) {
                if (_gameTipsClass != IntPtr.Zero && _isShowing == IntPtr.Zero) {
                    _isShowing = Il2CppReflection.FindMethodInHierarchy(_gameTipsClass, "get_isShowing", 0);
                }
                if (_isShowing == IntPtr.Zero) {
                    return false;
                }
                if (_gameTipsField == IntPtr.Zero || _generatedTipsPaneClass == IntPtr.Zero ||
                    _getGameTipsPane == IntPtr.Zero || _getGObjectVisible == IntPtr.Zero) {
                    return false;
                }
            }

            IntPtr tipsWindow = Il2CppReflection.GetFieldValue(manager, _gameTipsField);
            if (tipsWindow == IntPtr.Zero || !Il2CppReflection.InvokeMethodGetBool(_isShowing, tipsWindow)) {
                return true;
            }
            IntPtr pane = Il2CppReflection.InvokeMethod(_getGameTipsPane, tipsWindow, null);
            if (pane == IntPtr.Zero) {
                return false;
            }
            foreach (string fieldName in TransientTipFields) {
                IntPtr field = Il2CppReflection.FindField(_generatedTipsPaneClass, fieldName);
                if (field == IntPtr.Zero) {
                    continue;
                }
                IntPtr element = Il2CppReflection.GetFieldValue(pane, field);
                if (element != IntPtr.Zero && Il2CppReflection.InvokeMethodGetBool(_getGObjectVisible, element)) {
                    visible = true;
                    return true;
                }
            }
            return true;
        }

        // This covers SystemTipsWindow.showMessage's animation controller.
        public static bool TryIsGameToastVisible(out bool visible) {
            visible = false;
            if (!TryGetManager(out IntPtr manager)) {
                return false;
            }
            if (_tipsField == IntPtr.Zero) {
                _tipsField = Il2CppReflection.FindField(_managerClass, "_cachedSystemTipsWindow");
                _tipClass = Il2CppReflection.FindClass("UI.SystemTipsWindow");
                if (_tipClass != IntPtr.Zero) {
                    _getContentPane = Il2CppReflection.FindMethodInHierarchy(_tipClass, "get_contentPane", 0);
                    if (_isShowing == IntPtr.Zero) {
                        _isShowing = Il2CppReflection.FindMethodInHierarchy(_tipClass, "get_isShowing", 0);
                    }
                }
                IntPtr uiTipsClass = Il2CppReflection.FindClass("UI.UISystemTipsWindow");
                IntPtr controllerClass = Il2CppReflection.FindClass("FairyGUI.Controller");
                if (uiTipsClass != IntPtr.Zero) {
                    _showMessageField = Il2CppReflection.FindField(uiTipsClass, "showMessage");
                }
                if (controllerClass != IntPtr.Zero) {
                    _controllerIndexGetter = Il2CppReflection.FindMethodInHierarchy(
                        controllerClass, "get_selectedIndex", 0);
                }
            }
            if (_tipsField == IntPtr.Zero || _getContentPane == IntPtr.Zero ||
                _isShowing == IntPtr.Zero || _showMessageField == IntPtr.Zero ||
                _controllerIndexGetter == IntPtr.Zero) {
                return false;
            }
            IntPtr tips = Il2CppReflection.GetFieldValue(manager, _tipsField);
            if (tips == IntPtr.Zero || !Il2CppReflection.InvokeMethodGetBool(_isShowing, tips)) {
                return true;
            }
            IntPtr pane = Il2CppReflection.InvokeMethod(_getContentPane, tips, null);
            if (pane == IntPtr.Zero) {
                return false;
            }
            IntPtr controller = Il2CppReflection.GetFieldValue(pane, _showMessageField);
            if (controller == IntPtr.Zero) {
                return false;
            }
            visible = Il2CppReflection.InvokeMethodGetInt(_controllerIndexGetter, controller) == 1;
            return true;
        }
    }
}
