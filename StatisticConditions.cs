namespace AstralParty.Modding.SDK;

/// <summary>
/// Stable semantic achievement names verified against ConditionType.
/// Some are indexed and MUST use GetAchieveInfo_2 rather than the scalar API. Values match the hot-update
/// ConditionType enum; only the subset used by this SDK is defined here.
/// 热更新 ConditionType 的只读标量子集；不要将这些枚举值用于网络协议发送。
/// Changes to ConditionType require adapter verification, not silent renumbering.
/// </summary>
public enum StatisticConditionId {
    TotalKillCount = 2,
    TotalDirectDamage = 3,
    TotalDieCount = 4,
    TotalInjured = 5,
    TotalTrappedCount = 6,
    TotalSetTrapCount = 7,
    TotalSetBarricadeCount = 8,
    TotalSufferThunderCount = 9,
    TotalHospitalizedCount = 10,
    TotalWinLotteryCount = 11,
    TotalKillBossCount = 12,
    TotalWinGameCount = 13,
    TotalDoneGameCount = 14,
    TotalLoginDays = 15,
    TotalPlayDays = 16,
    TotalCharacterDone = 17,
    TotalCharacterWin = 18,
    TotalKillThiefCount = 19,
    TotalEatDumplingCount = 20,
    TotalAvoidTrainGames = 21,
    TotalGetThumbsUp = 22,
    TotalMapPlayCount = 23,
    TotalMapModePlayCount = 24,
    TotalBondLevelUpcount = 25,
    TotalPlayDaysPve = 26,
    TotalWinGameCountPve = 27,
    TotalRelicCountPve = 28,
    TotalWinGameCountPveAny = 29,
    TotalScratchoffCount = 30,
    TotalGet3GoldRelicInsaneTimes = 31,
    TotalSinkAllOpponentsTimes = 32,
    TotalWinGameCountStandard = 33,
    TotalWinGameCountUltra = 34,
    TotalWinGameCountAsymmetricalBattle = 35,
    TotalPvprankTimes = 36,
    TotalPvewinDifficulty1Times = 37,
    TotalPvewinDifficulty2OrHarderTimes = 38,
    TotalPvewinDifficultyTimes = 39,
    TotalPvewinDifficultyOrHarderTimes = 40,
    TotalMapWinTimes = 41,
    TotalKillUnitCount = 42,
    TotalPvewinDifficulty2Times = 43,
    TotalPvewinDifficulty3Times = 44,
    TotalPvewinDifficulty4Times = 45,
    TotalPvewinDifficulty5Times = 46,
    DragonBallSynthesis = 47,
    SpecifyMapWinGameCount = 48,
    SingleMaxKillCount = 100,
    SingleMaxPkDamage = 101,
    SingleMaxTotalGetGoldNum = 102,
    SingleMaxEatDumplingToHospitalCount = 103,
    SingleMaxProgress15WinPve = 104,
    SingleMaxNoDieWinPve = 105,
    SingleMaxQuickWinNightmare = 106,
    SingleMaxQuickWinInsane = 107,
    UseTraitorCardInsane = 108,
    NotAbsorbTeapotInsane = 109,
    TotalShopBuyCount = 311,
    GachaDraw = 315,
    TotalSingleModeWinTimes = 317,
    TotalSingleModeBulidingUpgrade = 318,
    TotalSingleModeRoundCount = 319
}

/// <summary>
/// Categories are UI metadata, not game enums. / 分类仅用于 UI 分组，不是游戏条件 ID。
/// </summary>
public static class StatisticCategories {
    public const string Combat = "combat";
    public const string Battle = "battle";
    public const string Career = "career";
    public const string Social = "social";
}
