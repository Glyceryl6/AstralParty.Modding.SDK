using System;
using System.Collections.Generic;
using System.Globalization;

namespace AstralParty.Modding.SDK {
    /// <summary>Owned and total counts; totals reflect the currently available item catalog.</summary>
    public readonly record struct OwnedTotal {
        public int Owned { get; }
        public int Total { get; }

        public OwnedTotal(int owned, int total) {
            if (owned < 0 || total < 0 || owned > total) {
                throw new ArgumentOutOfRangeException(nameof(owned),
                    "Owned count must be between zero and total count.");
            }
            Owned = owned;
            Total = total;
        }

        public override string ToString() =>
            Owned.ToString(CultureInfo.InvariantCulture) + "/" +
            Total.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// A read-only, per-hero account snapshot. A null field means unavailable or
    /// not applicable; it must never be displayed as an observed zero or false.
    /// Character titles and names are distinct localization values.
    /// </summary>
    public sealed record CharacterProfile(
        int EntityId, string Title, string Name, bool IsOwned,
        bool IsFavorite, int? PveLevel, int? PveExperience,
        int? BondLevel, int? BondExperience, bool? IsContracted,
        OwnedTotal? Skins, OwnedTotal? Expressions, OwnedTotal? PveTalents,
        int? BondExperienceRequired = null) {
        /// <summary>Localized name of the currently selected skin, if available.</summary>
        public string? EquippedSkinName { get; init; }
    }

    public interface ICharacterProfileReader {
        IReadOnlyList<CharacterProfile> ReadProfiles();
    }

    /// <summary>Portable presentation rules; no FairyGUI or game assemblies.</summary>
    public static class CharacterProfilePresentation {
        public static string Number(int? number) => number.HasValue
            ? number.Value.ToString(CultureInfo.InvariantCulture) : "—";

        public static string YesNo(bool? value) => value.HasValue
            ? (value.Value ? "是" : "否") : "—";

        public static string Progress(OwnedTotal? value) => value.HasValue
            ? value.Value.ToString() : "—";

        /// <summary>Current friendship EXP / EXP required for this level.</summary>
        public static string FriendshipExperience(int? current, int? required) {
            if (!current.HasValue || !required.HasValue || required.Value <= 0) {
                return "—";
            }
            return Number(current) + "/" + Number(required);
        }

        public static string SkinName(string? name) => string.IsNullOrWhiteSpace(name)
            ? "—" : name.Trim();
    }
}
