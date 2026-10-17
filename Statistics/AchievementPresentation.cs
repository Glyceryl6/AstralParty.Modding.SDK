using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AstralParty.Modding.SDK;

/// <summary>
/// A single achievement's condition, not a server counter. The caller resolves
/// the game-localized description and validates whether the condition uses
/// GetAchieveInfo_1 (scalar) or GetAchieveInfo_2 (indexed).
/// ParameterId must be preserved for indexed conditions: two characters/maps
/// using the same ConditionType are NOT the same statistic.
/// </summary>
public readonly record struct AchievementConditionSource(
    int ConditionId, int? ParameterId, string? LocalizedDescription);

/// <summary>A display label for a distinct condition key. No invented values.</summary>
public readonly record struct AchievementConditionLabel(
    int ConditionId, int? ParameterId, string Description);

/// <summary>
/// Pure-managed, version-neutral display helpers for the '成就数据' table.
/// This class does not read the game's tables or fake player progress.
/// </summary>
public static class AchievementPresentation {
    private static readonly Regex TrailingThreshold = new(
        @"\s*(?:≥|≧|>=|＞＝)\s*[+\-]?[\d０-９][\d０-９.,，]*(?:\s*(?:点|次|人|局|天|级|个|枚|份|星币|金币|分|场|颗|%|％))?\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Labels are based on the SDK's verified ConditionType ID snapshot and
    // the uploaded achievement descriptions. They are deliberately independent
    // of milestone thresholds, which share the same server counter.
    private static readonly IReadOnlyDictionary<int, string> KnownLabels =
        new Dictionary<int, string> {
            [(int)StatisticConditionId.TotalKillCount] = "累计击倒玩家",
            [(int)StatisticConditionId.TotalDirectDamage] = "累计直接伤害",
            [(int)StatisticConditionId.TotalDieCount] = "累计被击倒次数",
            [(int)StatisticConditionId.TotalInjured] = "累计承受伤害",
            [(int)StatisticConditionId.TotalTrappedCount] = "累计踩中陷阱次数",
            [(int)StatisticConditionId.TotalSetTrapCount] = "累计放置陷阱数量",
            [(int)StatisticConditionId.TotalSetBarricadeCount] = "累计放置路障数量",
            [(int)StatisticConditionId.TotalSufferThunderCount] = "累计被雷击次数",
            [(int)StatisticConditionId.TotalHospitalizedCount] = "累计住院次数",
            [(int)StatisticConditionId.TotalWinLotteryCount] = "累计彩票中奖次数",
            [(int)StatisticConditionId.TotalKillBossCount] = "累计击倒怪物次数",
            [(int)StatisticConditionId.TotalWinGameCount] = "累计胜利次数",
            [(int)StatisticConditionId.TotalDoneGameCount] = "累计参与派对次数",
            [(int)StatisticConditionId.TotalLoginDays] = "累计登录天数",
            [(int)StatisticConditionId.TotalKillThiefCount] = "累计击倒小偷次数",
            [(int)StatisticConditionId.TotalEatDumplingCount] = "累计吃团子次数",
            [(int)StatisticConditionId.TotalAvoidTrainGames] = "未被列车撞击的局数",
            [(int)StatisticConditionId.TotalGetThumbsUp] = "累计获得点赞次数",
            [(int)StatisticConditionId.SingleMaxKillCount] = "单局击倒玩家次数",
            [(int)StatisticConditionId.SingleMaxPkDamage] = "最高单次PK伤害",
            [(int)StatisticConditionId.SingleMaxTotalGetGoldNum] = "单局获得星币数量",
            [(int)StatisticConditionId.SingleMaxEatDumplingToHospitalCount] = "单局吃团子传送医院次数"
        };

    public static bool TryGetKnownLabel(int conditionId, out string label) {
        if (KnownLabels.TryGetValue(conditionId, out string? known)) {
            label = known;
            return true;
        }
        label = "";
        return false;
    }

    /// <summary>
    /// Removes ONLY an achievement's trailing '≥N[unit]' milestone. It does
    /// not strip other numbers, such as '合作挑战难度2' inside the description.
    /// </summary>
    public static string NormalizeDescription(string? description) {
        string text = (description ?? "").Trim();
        if (text.Length == 0) {
            return "";
        }
        return TrailingThreshold.Replace(text, "").TrimEnd(' ', '：', ':', '，', ',', '。');
    }

    /// <summary>
    /// Deduplicate by the real condition key, not the achievement ID/name.
    /// Preserves first appearance order. For unknown condition IDs select a
    /// normalized localized description; if several exist choose the shortest,
    /// then lexicographically, for consistent output across source enumeration.
    /// No value is created for missing server counters.
    /// </summary>
    public static IReadOnlyList<AchievementConditionLabel> UniqueConditions(
        IEnumerable<AchievementConditionSource> achievements) {
        ArgumentNullException.ThrowIfNull(achievements);
        var order = new List<(int ConditionId, int? ParameterId)>();
        var descriptions = new Dictionary<(int ConditionId, int? ParameterId), string>();
        foreach (AchievementConditionSource source in achievements) {
            if (source.ConditionId <= 0) {
                continue;
            }
            var key = (source.ConditionId, source.ParameterId);
            string normalized = NormalizeDescription(source.LocalizedDescription);
            if (!descriptions.TryGetValue(key, out string? existing)) {
                order.Add(key);
                descriptions.Add(key, normalized);
            } else if (normalized.Length > 0 && (existing.Length == 0 ||
                normalized.Length < existing.Length ||
                (normalized.Length == existing.Length &&
                 StringComparer.Ordinal.Compare(normalized, existing) < 0))) {
                descriptions[key] = normalized;
            }
        }
        return order.Select(key => new AchievementConditionLabel(
            key.ConditionId, key.ParameterId,
            TryGetKnownLabel(key.ConditionId, out string label)
                ? label
                : (descriptions[key].Length > 0 ? descriptions[key] : "—")))
            .ToArray();
    }
}
