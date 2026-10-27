namespace AstralParty.Modding.SDK;

/// <summary>Keep existing order, append validated distinct additions in key order; never modify the inputs.</summary>
public static class StableMerge {
    public static IReadOnlyList<T> AppendSafe<T, TKey>(IEnumerable<T> original, IEnumerable<T> additions,
        Func<T, TKey> key, Func<TKey, bool> validKey, Func<T, bool> eligible,
        IComparer<TKey>? ordering = null) where TKey : notnull {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(additions);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(validKey);
        ArgumentNullException.ThrowIfNull(eligible);
        var used = new HashSet<TKey>();
        var result = new List<T>();
        foreach (T value in original) {
            TKey id = key(value);
            if (validKey(id) && used.Add(id)) result.Add(value);
        }
        foreach (var value in additions.Select((item, index) => new { Item = item, Key = key(item), Index = index })
            .Where(item => validKey(item.Key) && !used.Contains(item.Key))
            .OrderBy(item => item.Key, ordering ?? Comparer<TKey>.Default).ThenBy(item => item.Index)) {
            if (used.Contains(value.Key) || !eligible(value.Item)) continue;
            used.Add(value.Key);
            result.Add(value.Item);
        }
        return result;
    }
}
