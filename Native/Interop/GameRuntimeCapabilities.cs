using System;
using AstralParty.Modding.SDK;

using AstralParty.Modding.SDK.Native.UI;

namespace AstralParty.Modding.SDK.Native.Interop {
    /// <summary>
    /// The SDK has no BepInEx/IL2CppSharp dependency. This is its game adapter.
    /// SDK 不引用游戏程序集；仅由宿主实现运行时能力检测。
    /// </summary>
    public sealed class GameRuntimeCapabilities : IRuntimeCapabilitySource {
        public bool HasType(string fullName) => FairyBridge.ClassOrZero(fullName) != IntPtr.Zero;

        public bool HasMethod(string type, string method, int parameterCount) =>
            FairyBridge.TryMethod(type, method, parameterCount) != IntPtr.Zero;

        public bool HasField(string type, string field) {
            try {
                return FairyBridge.Field(type, field) != IntPtr.Zero;
            } catch (MissingFieldException) {
                return false;
            } catch (TypeLoadException) {
                return false;
            }
        }
    }
}
