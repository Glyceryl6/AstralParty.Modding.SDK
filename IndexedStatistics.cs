using System;
using System.Collections.Generic;
using System.Linq;

namespace AstralParty.Modding.SDK;

/// <summary>Game condition-2 keys: Param1 identifies an entity, Param is its counter.</summary>
public static class IndexedStatisticConditions {
    public const int CharacterGames = (int)StatisticConditionId.TotalCharacterDone;
    public const int CharacterWins = (int)StatisticConditionId.TotalCharacterWin;
    public const int MapGames = (int)StatisticConditionId.TotalMapPlayCount;
    public const int MapWins = (int)StatisticConditionId.TotalMapWinTimes;
}

public enum StatisticEntityKind {
    Character = 1,
    Map = 2
}

/// <summary>
/// Title and Name are distinct game localization fields for characters:
/// Title = CharacterInfoConfigure.NickID, Name = CharacterInfoConfigure.NameID.
/// For maps Title is empty, Name comes from MapInfoConfigure.MapName.
/// Null count means the server condition is absent; do NOT silently show zero.
/// </summary>
public sealed record IndexedStatisticRow(StatisticEntityKind Kind, int EntityId,
    string Title, string Name, long? Games, long? Wins);

public interface IIndexedStatisticReader {
    IReadOnlyList<IndexedStatisticRow> ReadRows();
}

public static class IndexedStatistics {
    public static IReadOnlyList<IndexedStatisticRow> Present(IIndexedStatisticReader reader) {
        ArgumentNullException.ThrowIfNull(reader);
        return reader.ReadRows()
            .Where(row => row.Games.HasValue || row.Wins.HasValue)
            .OrderBy(row => row.Kind)
            .ThenBy(row => row.EntityId)
            .ToArray();
    }
}
