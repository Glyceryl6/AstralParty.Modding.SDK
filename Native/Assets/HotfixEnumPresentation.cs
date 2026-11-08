using System.Collections.Generic;

namespace AstralParty.Modding.SDK.Native {
    /// <summary>Runtime enum identifiers with field InspectorName labels.</summary>
    public static class HotfixEnumPresentation {
        public readonly record struct Entry(int Id, string Symbol, string Label);
        public static IReadOnlyList<Entry> Definitions(string enumName) {
            var rows = new List<Entry>();
            foreach (RuntimeEnumEntry entry in RuntimeEnumReflection.Entries(enumName))
                rows.Add(new Entry(entry.Id, entry.Name, InspectorEnumLabels.Label(enumName, entry.Name)));
            return rows;
        }
        public static string Label(string enumName, int id) {
            foreach (Entry entry in Definitions(enumName)) if (entry.Id == id) return entry.Label;
            return id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
