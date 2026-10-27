using System;
using System.Collections.Generic;

namespace AstralParty.Modding.SDK;

/// <summary>
/// Layout calculations for FairyGUI logical units. No Unity or native object
/// references are stored here; a game-version adapter owns the actual widgets.
/// 原生坐标排版只计算尺寸，IL2CPP/FairyGUI 控件仍由宿主负责。
/// </summary>
public readonly record struct UiLayoutRect(float X, float Y, float Width, float Height);

public static class NativeUiLayout {
    /// <summary>Fit authored artwork without changing its aspect ratio.</summary>
    public static float UniformScale(float nativeWidth, float nativeHeight, float width, float height) {
        if (!float.IsFinite(nativeWidth) || !float.IsFinite(nativeHeight) || !float.IsFinite(width) || !float.IsFinite(height) ||
            nativeWidth <= 0f || nativeHeight <= 0f || width <= 0f || height <= 0f)
            throw new ArgumentOutOfRangeException(nameof(width));
        return Math.Min(width / nativeWidth, height / nativeHeight);
    }

    /// <summary>Chinese-safe upper bound for a single-line caption.</summary>
    public static float EstimateCaptionWidth(string? text, float fontSize) {
        if (fontSize <= 0f) {
            throw new ArgumentOutOfRangeException(nameof(fontSize));
        }
        float units = 0f;
        foreach (char symbol in text ?? "") {
            units += symbol <= 0x7f ? 0.65f : 1.10f;
        }
        return units * fontSize;
    }

    /// <summary>
    /// Minimum width for outlined single-line text. Reserves glyph bearings,
    /// stroke and editor/runtime metrics differences. A host may enlarge the
    /// nine-slice background separately, using the same calculated width.
    /// </summary>
    public static float OutlineSafeCaptionWidth(string? text, float fontSize,
        float outlineWidth, float inset = 8f) {
        if (inset < 0f) {
            throw new ArgumentOutOfRangeException(nameof(inset));
        }
        return EstimateCaptionWidth(text, fontSize) * 1.16f +
            2f * (OutlinedCaptionPadding(fontSize, outlineWidth) + inset);
    }

    /// <summary>Native tabs are authored at their pre-animation position.</summary>
    public static float TabArtworkX(float nativeX, float columnOffset, float correction) {
        return nativeX + columnOffset + correction;
    }

    /// <summary>
    /// Reserve room for both outlines and CJK glyph bearings. A clipped text
    /// rectangle may lose the last character even when its font has that glyph.
    /// </summary>
    public static float OutlinedCaptionPadding(float fontSize, float outlineWidth) {
        if (fontSize <= 0f || outlineWidth < 0f) {
            throw new ArgumentOutOfRangeException(nameof(fontSize));
        }
        return Math.Max(6f, fontSize * 0.13f + outlineWidth * 2f);
    }

    /// <summary>Two compact, equally sized action buttons on a centered row.</summary>
    public static IReadOnlyList<UiLayoutRect> TwoActions(float availableWidth,
        float firstY, float rowHeight, float desiredWidth = 300f,
        float gap = 24f, float inset = 20f) {
        if (availableWidth <= 0f || desiredWidth <= 0f) {
            throw new ArgumentOutOfRangeException(nameof(availableWidth));
        }
        float natural = Math.Min(desiredWidth,
            Math.Max(1f, (availableWidth - 2f * inset - gap) * 0.5f));
        return CenteredFlow(new[] { natural, natural }, availableWidth,
            firstY, rowHeight, gap, 12f, inset);
    }

    /// <summary>Toggle width reserves room for the icon on its right.</summary>
    public static float ToggleWidth(string? text, float fontSize, float nativeWidth,
        float iconSpace = 70f, float horizontalInset = 16f) {
        return Math.Max(nativeWidth,
            EstimateCaptionWidth(text, fontSize) + iconSpace + horizontalInset * 2f);
    }

    /// <summary>
    /// Center each ROW of widgets, wrapping if the available width is smaller
    /// than the combined natural widths. Does not shrink CJK fonts to force fit.
    /// 按自然宽度水平居中，不够时换行，不通过缩小字体裁掉末尾汉字。
    /// </summary>
    public static IReadOnlyList<UiLayoutRect> CenteredFlow(IReadOnlyList<float> widths,
        float availableWidth, float firstY, float rowHeight, float gapX = 16f,
        float gapY = 12f, float inset = 12f) {
        ArgumentNullException.ThrowIfNull(widths);
        if (availableWidth <= 0f || rowHeight <= 0f || gapX < 0f || gapY < 0f || inset < 0f) {
            throw new ArgumentOutOfRangeException(nameof(availableWidth));
        }
        var result = new UiLayoutRect[widths.Count];
        float usableWidth = Math.Max(1f, availableWidth - 2f * inset);
        int start = 0;
        int row = 0;
        while (start < widths.Count) {
            int end = start;
            float total = 0f;
            while (end < widths.Count) {
                float natural = widths[end];
                if (natural <= 0f) {
                    throw new ArgumentOutOfRangeException(nameof(widths), "Widget widths must be positive");
                }
                float candidate = total + (end == start ? 0f : gapX) + natural;
                if (end > start && candidate > usableWidth) {
                    break;
                }
                total = candidate;
                end++;
            }
            float x = Math.Max(inset, (availableWidth - total) * 0.5f);
            for (int i = start; i < end; i++) {
                float width = Math.Min(widths[i], usableWidth);
                result[i] = new UiLayoutRect(x, firstY + row * (rowHeight + gapY),
                    width, rowHeight);
                x += widths[i] + gapX;
            }
            start = end;
            row++;
        }
        return result;
    }
}
