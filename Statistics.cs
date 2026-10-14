using System;
using System.Collections.Generic;

namespace AstralParty.Modding.SDK;

/// <summary>
/// Immutable condition definition: ID values are verified against the game's
/// ConditionType enum, not invented or inferred from achievement titles.
/// 统计条件 ID 以热更新 ConditionType 为准；本程序集不引用游戏 DLL。
/// </summary>
public sealed record StatisticDefinition(int ConditionId, string Key, string Group) {
    /// <summary>Readable typed view, preserving the existing integer API.</summary>
    public StatisticConditionId Condition => (StatisticConditionId)ConditionId;
}

public readonly record struct StatisticValue(StatisticDefinition Definition, long Value);

/// <summary>Read-only and optional: never use an absent value as an achievement completion signal.</summary>
public interface IStatisticReader {
    bool TryRead(int conditionId, out long value);
}

public static class StatisticCatalog {
    // Only scalar conditions served by TaskLogic.GetAchieveInfo_1 are listed.
    // Composite conditions (e.g., character/map indexed) require a separate
    // GetAchieveInfo_2 adapter and MUST NOT be flattened incorrectly.
    private static readonly StatisticDefinition[] Definitions = {
        new((int)StatisticConditionId.TotalKillCount, "astralprobe.stat.kills", StatisticCategories.Combat),
        new((int)StatisticConditionId.TotalDirectDamage, "astralprobe.stat.direct_damage", StatisticCategories.Combat),
        new((int)StatisticConditionId.TotalDieCount, "astralprobe.stat.deaths", StatisticCategories.Combat),
        new((int)StatisticConditionId.TotalInjured, "astralprobe.stat.damage_taken", StatisticCategories.Combat),
        new((int)StatisticConditionId.TotalTrappedCount, "astralprobe.stat.trapped", StatisticCategories.Battle),
        new((int)StatisticConditionId.TotalSetTrapCount, "astralprobe.stat.traps_placed", StatisticCategories.Battle),
        new((int)StatisticConditionId.TotalSetBarricadeCount, "astralprobe.stat.barricades_placed", StatisticCategories.Battle),
        new((int)StatisticConditionId.TotalSufferThunderCount, "astralprobe.stat.thunder_hits", StatisticCategories.Battle),
        new((int)StatisticConditionId.TotalHospitalizedCount, "astralprobe.stat.hospitalized", StatisticCategories.Battle),
        new((int)StatisticConditionId.TotalWinLotteryCount, "astralprobe.stat.lottery_wins", StatisticCategories.Battle),
        new((int)StatisticConditionId.TotalKillBossCount, "astralprobe.stat.boss_kills", StatisticCategories.Combat),
        new((int)StatisticConditionId.TotalWinGameCount, "astralprobe.stat.wins", StatisticCategories.Career),
        new((int)StatisticConditionId.TotalDoneGameCount, "astralprobe.stat.matches", StatisticCategories.Career),
        new((int)StatisticConditionId.TotalLoginDays, "astralprobe.stat.login_days", StatisticCategories.Career),
        new((int)StatisticConditionId.TotalPlayDays, "astralprobe.stat.play_days", StatisticCategories.Career),
        new((int)StatisticConditionId.TotalKillThiefCount, "astralprobe.stat.thief_kills", StatisticCategories.Combat),
        new((int)StatisticConditionId.TotalEatDumplingCount, "astralprobe.stat.dumplings", StatisticCategories.Battle),
        new((int)StatisticConditionId.TotalAvoidTrainGames, "astralprobe.stat.train_dodges", StatisticCategories.Battle),
        new((int)StatisticConditionId.TotalGetThumbsUp, "astralprobe.stat.likes_received", StatisticCategories.Social),
        new((int)StatisticConditionId.TotalPlayDaysPve, "astralprobe.stat.pve_days", StatisticCategories.Career),
        new((int)StatisticConditionId.TotalWinGameCountPve, "astralprobe.stat.pve_wins", StatisticCategories.Career),
        new((int)StatisticConditionId.TotalRelicCountPve, "astralprobe.stat.pve_relics", StatisticCategories.Battle),
        new((int)StatisticConditionId.TotalScratchoffCount, "astralprobe.stat.scratchoffs", StatisticCategories.Career),
        new((int)StatisticConditionId.TotalWinGameCountStandard, "astralprobe.stat.pvp_wins", StatisticCategories.Career),
        new((int)StatisticConditionId.TotalWinGameCountUltra, "astralprobe.stat.ultra_wins", StatisticCategories.Career),
        new((int)StatisticConditionId.TotalWinGameCountAsymmetricalBattle, "astralprobe.stat.gift_wins", StatisticCategories.Career),
        new((int)StatisticConditionId.TotalShopBuyCount, "astralprobe.stat.shop_purchases", StatisticCategories.Career),
        new((int)StatisticConditionId.GachaDraw, "astralprobe.stat.gacha_draws", StatisticCategories.Career),
        new((int)StatisticConditionId.TotalSingleModeWinTimes, "astralprobe.stat.single_wins", StatisticCategories.Career),
        new((int)StatisticConditionId.TotalSingleModeBulidingUpgrade, "astralprobe.stat.building_upgrades", StatisticCategories.Career),
        new((int)StatisticConditionId.TotalSingleModeRoundCount, "astralprobe.stat.single_rounds", "career"),
        new((int)StatisticConditionId.SingleMaxKillCount, "astralprobe.stat.single_max_kills", StatisticCategories.Combat),
        new((int)StatisticConditionId.SingleMaxPkDamage, "astralprobe.stat.single_max_pk_damage", StatisticCategories.Combat),
        new((int)StatisticConditionId.SingleMaxTotalGetGoldNum, "astralprobe.stat.single_max_gold", StatisticCategories.Career),
        new((int)StatisticConditionId.SingleMaxEatDumplingToHospitalCount, "astralprobe.stat.single_max_dumplings", StatisticCategories.Battle),
        new((int)StatisticConditionId.TotalBondLevelUpcount, "astralprobe.stat.bond_level_ups", StatisticCategories.Career)
    };

    public static IReadOnlyList<StatisticDefinition> All => Definitions;

    public static IReadOnlyList<StatisticValue> Read(IStatisticReader source) {
        ArgumentNullException.ThrowIfNull(source);
        var result = new List<StatisticValue>(Definitions.Length);
        foreach (StatisticDefinition definition in Definitions) {
            if (source.TryRead(definition.ConditionId, out long value)) {
                result.Add(new StatisticValue(definition, value));
            }
        }
        return result;
    }
}
