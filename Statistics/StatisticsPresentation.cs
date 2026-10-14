using System;
using System.Globalization;

namespace AstralParty.Modding.SDK;

/// <summary>
/// Pure-managed statistics pagination and invariant number formatting.
/// 不依赖 FairyGUI；行数与页码可供其他 MOD 复用。
/// </summary>
public static class StatisticsPresentation {
    public const int DefaultPageSize = 10; // UI.GachaInfoWindow.recordMaxNum.

    public static string FormatCount(long value) =>
        value.ToString(CultureInfo.InvariantCulture);

    public static int PageCount(int rowCount, int pageSize = DefaultPageSize) {
        if (pageSize <= 0) {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }
        int count = Math.Max(0, rowCount);
        // Avoid rowCount + pageSize - 1 overflowing when count == int.MaxValue.
        return count == 0 ? 1 : 1 + (count - 1) / pageSize;
    }

    public static int ClampPage(int page, int rowCount, int pageSize = DefaultPageSize) =>
        Math.Clamp(page, 0, PageCount(rowCount, pageSize) - 1);

    public static StatisticPageSlice Slice(int page, int rowCount, int pageSize = DefaultPageSize) {
        int count = Math.Max(0, rowCount);
        int pages = PageCount(count, pageSize);
        int selected = Math.Clamp(page, 0, pages - 1);
        int start = selected * pageSize;
        return new StatisticPageSlice(selected, pages, start, Math.Min(pageSize, count - start));
    }
}

/// <summary>Zero-based visible window into a flat list; page display numbers are one-based.</summary>
public readonly record struct StatisticPageSlice(int Page, int PageCount, int Start, int Count) {
    public int DisplayPage => Page + 1;
    public bool HasPrevious => Page > 0;
    public bool HasNext => Page + 1 < PageCount;
}
