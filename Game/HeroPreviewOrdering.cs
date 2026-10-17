using System;
using System.Collections.Generic;
using System.Linq;

namespace AstralParty.Modding.SDK;

/// <summary>
/// A pure-managed plan for inserting additional local-only hero preview rows.
/// Does not unlock ownership, modify IsClientShow/IsVailItem, or load assets.
/// The host MUST acquire a fresh vanilla GetHeroCards(checkStatus: true) list
/// on each refresh instead of feeding a previously patched heroCards list back
/// as the original. Host checks for a real HeroCardData, STRCharacter record,
/// CharacterInfoConfigure, and resolvable Addressables portrait before adding.
/// </summary>
public static class HeroPreviewOrdering {
    /// <summary>
    /// Preserve the game's visible order exactly (first valid ID wins), append
    /// safe additional entries in stable hero-ID order, never append duplicates.
    /// The predicate runs on the Unity main thread in the host integration.
    /// </summary>
    public static IReadOnlyList<T> Merge<T>(
        IEnumerable<T> vanillaVisible,
        IEnumerable<T> extraCandidates,
        Func<T, int> heroId,
        Func<T, bool> isSafeExtra) {
        ArgumentNullException.ThrowIfNull(vanillaVisible);
        ArgumentNullException.ThrowIfNull(extraCandidates);
        ArgumentNullException.ThrowIfNull(heroId);
        ArgumentNullException.ThrowIfNull(isSafeExtra);

        var output = new List<T>();
        var used = new HashSet<int>();
        foreach (T value in vanillaVisible) {
            int id = heroId(value);
            if (id > 0 && used.Add(id)) {
                output.Add(value);
            }
        }

        // Snapshot and sort additional entries without mutating the game's
        // source list, then check status/resources only for truly new IDs.
        foreach (var candidate in extraCandidates
            .Select((item, index) => new { Item = item, Id = heroId(item), Index = index })
            .Where(x => x.Id > 0 && !used.Contains(x.Id))
            .OrderBy(x => x.Id)
            .ThenBy(x => x.Index)) {
            if (!used.Contains(candidate.Id) && isSafeExtra(candidate.Item)) {
                used.Add(candidate.Id);
                output.Add(candidate.Item);
            }
        }
        return output;
    }
}
