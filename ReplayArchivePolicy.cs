using System;

namespace AstralParty.Modding.SDK;

/// <summary>
/// Client-side replay archive safety rules. The game's actual replay capacity
/// comes from StaticGlobalData.MAX_BATTLE_RECORDS (runtime configuration),
/// so the default is only a fallback for hosts that cannot read that setting.
/// 回放数量是游戏运行时配置，不应把默认值当成协议常量。
/// </summary>
public static class ReplayArchivePolicy {
    public const int DefaultNativeRecordCapacity = 10;
    public const int MinimumVisibleSlots = 2;
    public const int ReservedFreeSlots = 1;

    public static int VisibleSlots(int requested, int nativeCapacity = DefaultNativeRecordCapacity) {
        if (nativeCapacity <= ReservedFreeSlots) {
            throw new ArgumentOutOfRangeException(nameof(nativeCapacity));
        }
        int maximum = nativeCapacity - ReservedFreeSlots;
        return Math.Clamp(requested, Math.Min(MinimumVisibleSlots, maximum), maximum);
    }
}
