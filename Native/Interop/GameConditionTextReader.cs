using AstralParty.Modding.SDK.Native;
using System;
using System.Collections.Generic;
using IL2CppSharp;

using AstralParty.Modding.SDK.Native.Interop;
using AstralParty.Modding.SDK.Native.UI;

namespace AstralParty.Modding.SDK.Native.Interop {
    /// <summary>
    /// Join ConditionType -> task configuration -> NameID/DescId -> matching STR.LocalDict.
    /// A STR key is never inferred from an enum value. Related task text is not an enum definition.
    /// StaticConfigure already parsed the game's .bytes via its generated protobuf Parser.
    /// </summary>
    public static class GameConditionTextReader {
        private const int MaxRows = 8192;
        private static readonly HashSet<string> Warned = new(StringComparer.Ordinal);

        public static string Local(string table, int textId) {
            if (textId <= 0) return "";
            try {
                IntPtr row = NativeCatalogLookup.GetById("STR" + table,
                    "STR" + table + "Configure", "LocalDict", textId);
                if (row == IntPtr.Zero) return "";
                string type = "STR" + table + "LocalConfigure";
                IntPtr method = FairyBridge.TryMethod(type, "GetLocal", 0);
                string localized = method == IntPtr.Zero ? "" : Il2CppReflection.InvokeGetString(method, row);
                if (!string.IsNullOrWhiteSpace(localized)) return localized;
                // Empty/uninitialized language selection must not result in a native id=0 query.
                string simplified = FairyBridge.GetString(row, type, "Simplified");
                return string.IsNullOrWhiteSpace(simplified) ? FairyBridge.GetString(row, type, "English") : simplified;
            } catch (Exception ex) { Warn(table, ex); return ""; }
        }

        public static string Template(IntPtr row, string type, string table) {
            if (row == IntPtr.Zero) return "";
            int descriptionId = FairyBridge.GetInt(row, type, "DescId");
            string description = Local(table, descriptionId);
            return string.IsNullOrWhiteSpace(description)
                ? Local(table, FairyBridge.GetInt(row, type, "NameID")) : description;
        }

        public static IReadOnlyDictionary<int, string> ConditionExamples(bool includeText = true) {
            var candidates = new Dictionary<int, SortedSet<string>>();
            ReadSource("Activity", "ActivityConfigure", "Tasks", "ActivityTaskConfigure", "Activity", candidates, includeText);
            ReadSource("Task", "TaskConfigure", "Weeklys", "TaskWeeklyConfigure", "Task", candidates, includeText);
            ReadSource("Task", "TaskConfigure", "Beginners", "TaskBeginnerConfigure", "Task", candidates, includeText);
            ReadSource("Task", "TaskConfigure", "Day7S", "TaskDay7Configure", "Task", candidates, includeText);
            ReadSource("Achieve", "AchieveConfigure", "Globals", "AchieveGlobalConfigure", "Achieve", candidates, includeText);
            var result = new Dictionary<int, string>();
            foreach (var pair in candidates) {
                string? shortest = null;
                foreach (string text in pair.Value)
                    if (shortest == null || text.Length < shortest.Length) shortest = text;
                result[pair.Key] = shortest ?? "";
            }
            return result;
        }

        private static void ReadSource(string catalog, string container, string collection,
            string rowType, string strTable, Dictionary<int, SortedSet<string>> result, bool includeText) {
            try {
                IntPtr configure = IL2CPP.Class("StaticConfigure").Method("get_" + catalog).Args().InvokeStatic();
                if (configure == IntPtr.Zero) return;
                IntPtr rows = FairyBridge.GetObject(configure, container, collection);
                int count = NativeValue.ListCount(rows);
                if (count < 0 || count > MaxRows) return;
                for (int i = 0; i < count; i++) {
                    IntPtr row = NativeValue.ListItem(rows, i);
                    if (row == IntPtr.Zero) continue;
                    int condition = FairyBridge.GetInt(row, rowType, "ConditionType");
                    if (condition <= 0) continue;
                    // Keep the observed condition ID even when its STR table has not loaded.
                    if (!result.TryGetValue(condition, out var texts)) {
                        texts = new SortedSet<string>(StringComparer.Ordinal);
                        result.Add(condition, texts);
                    }
                    string text = includeText ? Template(row, rowType, strTable) : "";
                    if (!string.IsNullOrWhiteSpace(text)) texts.Add(text);
                }
            } catch (Exception ex) { Warn(catalog + "." + collection, ex); }
        }

        private static void Warn(string key, Exception ex) {
            if (Warned.Add(key)) NativeSdkServices.Warn("[stats/str] " + key + ": " + ex);
        }
    }
}
