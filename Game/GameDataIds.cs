namespace AstralParty.Modding.SDK;

/// <summary>
/// Read-only ID snapshots from AstralParty.Runtime's generated protobuf enums.
/// 对照热更新源码的只读值映射；新游戏版本可能更改，使用前应核对。
/// Do not send these SDK enums as RPC arguments without verifying the active game build.
/// </summary>
public enum GameMapModeId {
    None = 0,
    Standard = 1,
    Novice = 2,
    Ultra = 3,
    Pve = 4,
    CampaignPvp = 5,
    CampaignPve = 6,
    AsymmetricalBattle = 7,
    PracticePvp = 8,
    PracticePve = 9,
    PveNovice = 10,
    LuckyStarBattle = 11,
    MutatorPve = 12
}

public enum GameLanguageId {
    None = 0,
    English = 10,
    Japanese = 22,
    Korean = 23,
    SimplifiedChinese = 40,
    TraditionalChinese = 41
}

public enum GameItemQualityId {
    None = 0,
    White = 1,
    Green = 2,
    Blue = 3,
    Purple = 4,
    Orange = 5
}

public enum GameGachaTableId {
    None = 0,
    Role = 1,
    Skin = 2,
    Activity = 3,
    Replica = 4
}

/// <summary>Core and hot-update CharacterType values; previews only, never network writes.</summary>
public enum GameCharacterTypeId {
    None = 0,
    Hero = 1,
    Monster = 2
}

/// <summary>UI.UIStringType localization tables (not on-the-wire identifiers).</summary>
public enum GameLocalizationTableId {
    // Verified against 2026-10-16 UI.UIStringType. The game inserted
    // another table before Character; old values queried STRCard.
    // 2026-10-16 热更枚举值；旧 Character=10 会错查 STRCard。
    Card = 10,
    Character = 11,
    Item = 29,
    Map = 32
}
