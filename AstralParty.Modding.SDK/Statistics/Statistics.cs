using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AstralParty.Modding.SDK;

/// <summary>
/// Immutable, game-source-verified condition ID and host-neutral label key.
/// Each MOD chooses how to localize a key; none depend on AstralProbe resources.
/// </summary>
public sealed record StatisticDefinition(int ConditionId, string Key, string Group) {
    public StatisticConditionId Condition => (StatisticConditionId)ConditionId;
}

public readonly record struct StatisticValue(StatisticDefinition Definition, long Value);

public interface IStatisticReader {
    bool TryRead(int conditionId, out long value);
}

/// <summary>
/// Extensible key-to-definition registry. Built-ins contain only scalar
/// GetAchieveInfo_1 conditions. Indexed character/map conditions belong to
/// IndexedStatistics and cannot be silently flattened.
/// </summary>
public static class StatisticCatalog {
    public const string KeyPrefix = "astralparty.stats.";
    private static readonly object Gate = new();
    private static readonly Dictionary<int, StatisticDefinition> Registry = CreateBuiltIns();

    private static Dictionary<int, StatisticDefinition> CreateBuiltIns() {
        var values = new Dictionary<int, StatisticDefinition>();
        void Add(StatisticConditionId id, string label, string group) {
            values.Add((int)id, new StatisticDefinition((int)id, KeyPrefix + label, group));
        }
        Add(StatisticConditionId.TotalKillCount, "kills", StatisticCategories.Combat);
        Add(StatisticConditionId.TotalDirectDamage, "direct_damage", StatisticCategories.Combat);
        Add(StatisticConditionId.TotalDieCount, "deaths", StatisticCategories.Combat);
        Add(StatisticConditionId.TotalInjured, "damage_taken", StatisticCategories.Combat);
        Add(StatisticConditionId.TotalTrappedCount, "trapped", StatisticCategories.Battle);
        Add(StatisticConditionId.TotalSetTrapCount, "traps_placed", StatisticCategories.Battle);
        Add(StatisticConditionId.TotalSetBarricadeCount, "barricades_placed", StatisticCategories.Battle);
        Add(StatisticConditionId.TotalSufferThunderCount, "thunder_hits", StatisticCategories.Battle);
        Add(StatisticConditionId.TotalHospitalizedCount, "hospitalized", StatisticCategories.Battle);
        Add(StatisticConditionId.TotalWinLotteryCount, "lottery_wins", StatisticCategories.Battle);
        Add(StatisticConditionId.TotalKillBossCount, "boss_kills", StatisticCategories.Combat);
        Add(StatisticConditionId.TotalWinGameCount, "wins", StatisticCategories.Career);
        Add(StatisticConditionId.TotalDoneGameCount, "matches", StatisticCategories.Career);
        Add(StatisticConditionId.TotalLoginDays, "login_days", StatisticCategories.Career);
        Add(StatisticConditionId.TotalPlayDays, "play_days", StatisticCategories.Career);
        Add(StatisticConditionId.TotalKillThiefCount, "thief_kills", StatisticCategories.Combat);
        Add(StatisticConditionId.TotalEatDumplingCount, "dumplings", StatisticCategories.Battle);
        Add(StatisticConditionId.TotalAvoidTrainGames, "train_dodges", StatisticCategories.Battle);
        Add(StatisticConditionId.TotalGetThumbsUp, "likes_received", StatisticCategories.Social);
        Add(StatisticConditionId.TotalBondLevelUpcount, "bond_levelups", StatisticCategories.Social);
        Add(StatisticConditionId.TotalWinGameCountPveAny, "pve_any_wins", StatisticCategories.Career);
        Add(StatisticConditionId.TotalSinkAllOpponentsTimes, "all_opponents_down", StatisticCategories.Combat);
        Add(StatisticConditionId.DragonBallSynthesis, "dragonball_synthesis", StatisticCategories.Battle);
        Add(StatisticConditionId.TotalPlayDaysPve, "pve_days", StatisticCategories.Career);
        Add(StatisticConditionId.TotalWinGameCountPve, "pve_wins", StatisticCategories.Career);
        Add(StatisticConditionId.TotalRelicCountPve, "pve_relics", StatisticCategories.Battle);
        Add(StatisticConditionId.TotalScratchoffCount, "scratchoffs", StatisticCategories.Career);
        Add(StatisticConditionId.TotalWinGameCountStandard, "pvp_wins", StatisticCategories.Career);
        Add(StatisticConditionId.TotalWinGameCountUltra, "ultra_wins", StatisticCategories.Career);
        Add(StatisticConditionId.TotalWinGameCountAsymmetricalBattle, "gift_wins", StatisticCategories.Career);
        Add(StatisticConditionId.TotalShopBuyCount, "shop_purchases", StatisticCategories.Career);
        Add(StatisticConditionId.GachaDraw, "gacha_draws", StatisticCategories.Career);
        Add(StatisticConditionId.TotalSingleModeWinTimes, "single_wins", StatisticCategories.Career);
        Add(StatisticConditionId.TotalSingleModeBulidingUpgrade, "building_upgrades", StatisticCategories.Career);
        Add(StatisticConditionId.TotalSingleModeRoundCount, "single_rounds", "career");
        Add(StatisticConditionId.SingleMaxKillCount, "single_max_kills", StatisticCategories.Combat);
        Add(StatisticConditionId.SingleMaxPkDamage, "single_max_pk_damage", StatisticCategories.Combat);
        Add(StatisticConditionId.SingleMaxTotalGetGoldNum, "single_max_gold", StatisticCategories.Career);
        Add(StatisticConditionId.SingleMaxEatDumplingToHospitalCount, "single_max_dumplings", StatisticCategories.Battle);
        return values;
    }

    /// <summary>
    /// One-time registration for additional version-checked or custom scalar
    /// conditions. The caller must use its own namespaced localization key.
    /// Existing game IDs cannot be silently replaced by another plugin.
    /// </summary>
    public static bool TryRegister(StatisticDefinition definition) {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.ConditionId < 0 || string.IsNullOrWhiteSpace(definition.Group) ||
            string.IsNullOrWhiteSpace(definition.Key) || !definition.Key.Contains('.')) {
            return false;
        }
        lock (Gate) {
            return Registry.TryAdd(definition.ConditionId, definition);
        }
    }

    public static bool TryGet(int conditionId, out StatisticDefinition? definition) {
        lock (Gate) {
            return Registry.TryGetValue(conditionId, out definition);
        }
    }

    public static IReadOnlyList<StatisticDefinition> All {
        get {
            lock (Gate) {
                return Array.AsReadOnly(Registry.Values.ToArray());
            }
        }
    }

    public static IReadOnlyDictionary<int, StatisticDefinition> ById {
        get {
            lock (Gate) {
                return new ReadOnlyDictionary<int, StatisticDefinition>(
                    new Dictionary<int, StatisticDefinition>(Registry));
            }
        }
    }

    public static IReadOnlyList<StatisticValue> Read(IStatisticReader source) {
        ArgumentNullException.ThrowIfNull(source);
        IReadOnlyList<StatisticDefinition> snapshot = All;
        var result = new List<StatisticValue>(snapshot.Count);
        foreach (StatisticDefinition definition in snapshot) {
            if (source.TryRead(definition.ConditionId, out long value)) {
                result.Add(new StatisticValue(definition, value));
            }
        }
        return result;
    }
}
