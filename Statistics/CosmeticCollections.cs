using System;
using System.Collections.Generic;

namespace AstralParty.Modding.SDK;

/// <summary>Account cosmetics are counted by client item category, not individual hero.</summary>
public readonly record struct CosmeticCollectionRow(int ItemType, string LabelKey, OwnedTotal Progress);

/// <summary>Read-only contract; filtering and bag lookups belong to a versioned host.</summary>
public interface IAccountCosmeticReader {
    IReadOnlyList<CosmeticCollectionRow> ReadCollections();
}

/// <summary>Source-verified ItemType IDs; keep in step with the game's latest hotfix.</summary>
public static class CosmeticItemTypes {
    public const int AccountHeadShot = 9;
    public const int AccountBackground = 10;
    public const int Dice = 11;
    public const int Mouse = 12;
    public const int CardFrame = 13;
    public const int CardBack = 14;
    public const int Bgm = 15;
    public const int Effect = 22;
    public const int HeroStandingPainting = 23;
    public const int HeroHeadShot = 24;
    public const int HeroExpression = 25;
    public const int HeroSelectAnimation = 26;
    public const int SkinFittings = 30;
    public const int Kv = 32;
    public const int AltArt = 33;
}
