using System;
using IL2CppSharp;

using AstralParty.Modding.SDK.Native.UI;

namespace AstralParty.Modding.SDK.Native.Interop {
    /// <summary>Read-only game singleton/UI helpers, all on the Unity main thread.
    /// 仅读取游戏单例与 UI；禁止在后台线程调用 IL2CPP 对象。
    /// </summary>
    public static class NativeGame {
        private static IntPtr _managerClass;
        private static IntPtr _managerProvider;
        private static IntPtr _logicProvider;
        private static IntPtr _logicClass;

        public static bool TryUiManager(out IntPtr manager) {
            manager = IntPtr.Zero;
            if (!FairyBridge.Ready()) return false;
            if (_managerClass == IntPtr.Zero) {
                _managerClass = FairyBridge.ClassOrZero("UI.UIManager");
                if (_managerClass != IntPtr.Zero)
                    _managerProvider = Il2CppAPI.GetParentClass(_managerClass);
            }
            if (_managerProvider == IntPtr.Zero) return false;
            manager = Il2CppReflection.GetStaticFieldValue(_managerProvider, "_inst");
            return manager != IntPtr.Zero;
        }

        public static bool TrySelfPlayer(out IntPtr player) {
            player = IntPtr.Zero;
            if (!FairyBridge.Ready()) return false;
            if (_logicClass == IntPtr.Zero) {
                _logicClass = FairyBridge.ClassOrZero("GameLogic.GameLogicManager");
                if (_logicClass != IntPtr.Zero)
                    _logicProvider = Il2CppAPI.GetParentClass(_logicClass);
            }
            if (_logicProvider == IntPtr.Zero) return false;
            IntPtr logic = Il2CppReflection.GetStaticFieldValue(_logicProvider, "_inst");
            if (logic == IntPtr.Zero) return false;
            IntPtr battle = FairyBridge.GetObject(logic, "GameLogic.GameLogicManager", "battle");
            if (battle == IntPtr.Zero) return false;
            IntPtr method = FairyBridge.TryMethod("GameLogic.BattleLogic", "GetSelfPlayerData", 0);
            if (method == IntPtr.Zero) return false;
            player = Il2CppReflection.InvokeMethod(method, battle, null);
            return player != IntPtr.Zero;
        }

        public static IntPtr Singleton(string type) {
            IntPtr klass = FairyBridge.ClassOrZero(type);
            if (klass == IntPtr.Zero) return IntPtr.Zero;
            IntPtr parent = Il2CppAPI.GetParentClass(klass);
            return parent == IntPtr.Zero ? IntPtr.Zero :
                Il2CppReflection.GetStaticFieldValue(parent, "_inst");
        }

        public static IntPtr CachedWindow(IntPtr manager, string field) {
            return manager == IntPtr.Zero ? IntPtr.Zero : FairyBridge.FieldValue(manager, "UI.UIManager", field);
        }

        public static bool IsShowing(IntPtr window) {
            return window != IntPtr.Zero && FairyBridge.GetBool(window, "FairyGUI.Window", "isShowing");
        }

        public static IntPtr WindowPane(IntPtr window) {
            return FairyBridge.GetObject(window, "FairyGUI.Window", "contentPane");
        }

        public static bool IsBattlePanel(IntPtr manager) {
            // UIManager.currentPanel changes from BattleInfoPanel to HandCardPanel
            // during BattleLogic.ReadyBattleUI(). Testing only BattleInfoPanel
            // rejects EVERY shortcut in a normal battle after hand cards load.
            // 原版加载手牌后 currentPanel 变成 HandCardPanel；不能只判断 BattleInfoPanel。
            if (manager == IntPtr.Zero) return false;
            IntPtr sceneClass = FairyBridge.ClassOrZero("Core.Scene.BattleSceneController");
            if (sceneClass == IntPtr.Zero ||
                Il2CppReflection.GetStaticFieldValue(sceneClass, "inst") == IntPtr.Zero)
                return false;
            IntPtr panel = FairyBridge.GetObject(manager, "UI.UIManager", "currentPanel");
            return panel != IntPtr.Zero &&
                !IsInstanceOf(panel, "UI.HomePanel") &&
                (IsInstanceOf(panel, "UI.BattleInfoPanel") ||
                 IsInstanceOf(panel, "UI.HandCardPanel") ||
                 IsInstanceOf(panel, "UI.BattleSettlementPanel"));
        }

        public static bool IsInstanceOf(IntPtr instance, string typeName) {
            IntPtr target = FairyBridge.ClassOrZero(typeName);
            if (instance == IntPtr.Zero || target == IntPtr.Zero) return false;
            for (IntPtr klass = Il2CppReflection.GetObjectClass(instance);
                klass != IntPtr.Zero; klass = Il2CppAPI.GetParentClass(klass)) {
                if (klass == target) return true;
            }
            return false;
        }

        public static bool IsVisible(IntPtr obj) {
            return obj != IntPtr.Zero && FairyBridge.GetBool(obj, "FairyGUI.GObject", "visible");
        }

        public static bool IsOnStage(IntPtr obj) {
            return obj != IntPtr.Zero && FairyBridge.GetBool(obj, "FairyGUI.GObject", "onStage");
        }

        public static void Click(IntPtr button) {
            if (button == IntPtr.Zero) return;
            IntPtr listener = FairyBridge.GetObject(button, "FairyGUI.GObject", "onClick");
            if (listener == IntPtr.Zero) return;
            // Invoke the game's own listener; never synthesize a server packet.
            // 只触发原版注册的回调，避免自造网络请求。
            Il2CppReflection.InvokeMethod(
                FairyBridge.Method("FairyGUI.EventListener", "Call", 0), listener, Array.Empty<IntPtr>());
        }

        public static int ControllerIndex(IntPtr controller) {
            return FairyBridge.GetInt(controller, "FairyGUI.Controller", "selectedIndex");
        }

        public static bool TryReadGold(IntPtr player, out int gold) {
            gold = 0;
            if (player == IntPtr.Zero) return false;
            IntPtr property = FairyBridge.GetObject(player, "GameLogic.BattlePlayerData", "Property");
            if (property == IntPtr.Zero) return false;
            IntPtr reactive = FairyBridge.FieldValue(property, "Core.Unit.BattleProperty", "gold");
            if (reactive == IntPtr.Zero) return false;
            // ReactiveProperty<int>.Value holds replicated server-authoritative gold.
            // Value 为客户端已同步的服务端星币；未读到时绝不自动跳过。
            IntPtr getter = Il2CppReflection.FindMethodInHierarchy(
                Il2CppReflection.GetObjectClass(reactive), "get_Value", 0);
            if (getter == IntPtr.Zero) return false;
            gold = Il2CppReflection.InvokeGet<int>(getter, reactive);
            return gold >= 0;
        }
    }
}
