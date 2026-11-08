using AstralParty.Modding.SDK.Native;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using AstralParty.Modding.SDK;
using IL2CppSharp;


namespace AstralParty.Modding.SDK.Native.UI {
    /// <summary>
    /// A single self-contained popup styled after the gacha history table.
    /// It deliberately does not clone GachaInfoWindow: that scene-owned prefab
    /// is unavailable until the player visits the gacha screen and its native
    /// controller/artwork can be re-initialized during scene changes.
    /// 独立复刻表格外观，绝不依赖抽奖资源包的加载时机。
    /// </summary>
    public sealed class StatisticsHistoryView {
        public const int RowsPerPage = StatisticsPresentation.DefaultPageSize;
        private const int MaxColumns = 16;
        private readonly int _maxColumns;
        private readonly bool _wideCharacter;
        private const float HorizontalPadding = 44f;
        private const float DefaultHeaderY = 112f;
        private const float HeaderHeight = 42f;
        private const float DefaultRowsY = 162f;
        private readonly float _width;
        private readonly float _height;
        private readonly float _headerY;
        private readonly float _rowsY;
        private readonly List<IntPtr> _characterTabs = new List<IntPtr>();
        private readonly List<IntPtr> _characterTabCaptions = new List<IntPtr>();
        private readonly string _emptyKey;
        private readonly List<IntPtr[]> _rows = new List<IntPtr[]>();
        private readonly List<IntPtr> _headers = new List<IntPtr>();

        private readonly IntPtr _parent;
        private readonly List<IntPtr> _rowActions = new();
        private readonly IntPtr _titleText;
        private readonly IntPtr _emptyText;
        private readonly IntPtr _pageLabel;
        private readonly IntPtr _prevButton;
        private readonly IntPtr _nextButton;
        private readonly IntPtr _closeButton;
        private int _columnCount = MaxColumns;

        private static readonly string[] DefaultHeaders = {
            "astralprobe.stats.column.group", "astralprobe.stats.column.name",
            "astralprobe.stats.column.value", "astralprobe.stats.column.id"
        };

        public IntPtr Previous => _prevButton;
        public IntPtr Next => _nextButton;
        public IntPtr CloseButton => _closeButton;
        public IntPtr CharacterTab(int index) => index >= 0 && index < _characterTabs.Count
            ? _characterTabs[index] : IntPtr.Zero;

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct NativeColor {
            public readonly float R;
            public readonly float G;
            public readonly float B;
            public readonly float A;

            public NativeColor(uint rgb) {
                R = ((rgb >> 16) & 255) / 255f;
                G = ((rgb >> 8) & 255) / 255f;
                B = (rgb & 255) / 255f;
                A = 1f;
            }
        }

        public StatisticsHistoryView(IntPtr parent, float width, float height,
            string titleKey = "astralprobe.stats.title",
            string emptyKey = "astralprobe.stats.empty", bool characterTabs = false,
            bool detailTabs = false, bool wideCharacter = false) {
            _parent = parent;
            _emptyKey = emptyKey;
            _wideCharacter = wideCharacter;
            _maxColumns = wideCharacter ? 13 : 7;
            _width = width;
            _height = height;
            _headerY = characterTabs || detailTabs ? 148f : DefaultHeaderY;
            _rowsY = characterTabs || detailTabs ? 196f : DefaultRowsY;
            // Own only the contents of the caller's popup panel. No second
            // native Gacha window or separately positioned fallback layer.
            Rectangle(parent, 0f, 0f, width, height, 0x303030, 0xFFFAF0);
            Rectangle(parent, 0f, 0f, width, 70f, 0x171717, 0x171717);
            Rectangle(parent, HorizontalPadding, _headerY, width - 2f * HorizontalPadding,
                HeaderHeight, 0xC2C2C2, 0xEBEBEB);

            // Align to the FULL title bar, not the table headers or close-button
            // area. The X/Y offset is symmetric even when the dialog is scaled.
            IntPtr title = FairyBridge.AddText(parent, NativeSdkServices.Translate(titleKey),
                35, 0xFFFFFF, 0f, 4f, width, 62f);
            _titleText = title;
            PrepareText(title);

            for (int col = 0; col < _maxColumns; col++) {
                IntPtr header = FairyBridge.AddText(parent, "", 23, 0x65432F,
                    0f, _headerY, 40f, HeaderHeight);
                PrepareText(header);
                // The column allocator expands the cell instead of reducing font size.
                // The old Shrink setting made long skin/friendship values smaller.
                if (_wideCharacter) {
                    FairyBridge.SetInt(header, "FairyGUI.GTextField", "autoSize", 0);
                }
                _headers.Add(header);
            }
            float rowHeight = RowHeight();
            // The whole table uses a single stable visual style. Never lift
            // GachaInfo row/page controls: their artwork and state change when
            // the original history screen is opened in another scene.
            for (int row = 0; row < RowsPerPage; row++) {
                Rectangle(parent, HorizontalPadding, _rowsY + row * rowHeight,
                    width - 2f * HorizontalPadding, rowHeight,
                    row % 2 == 0 ? 0xEAE3D8u : 0xF3ECE1u,
                    row % 2 == 0 ? 0xF3EEE6u : 0xFFFAF0u);
                IntPtr[] cells = new IntPtr[_maxColumns];
                for (int col = 0; col < cells.Length; col++) {
                    cells[col] = FairyBridge.AddText(parent, "",
                        22, 0x463C35,
                        0f, _rowsY + row * rowHeight, 40f, rowHeight);
                    PrepareText(cells[col]);
                    if (_wideCharacter) {
                        // Keep the same fixed 22pt font for every character row.
                        // Column sizing, not text shrinking, handles long values.
                        FairyBridge.SetInt(cells[col], "FairyGUI.GTextField", "autoSize", 0);
                        if (col == 7) {
                            FairyBridge.SetBool(cells[col], "FairyGUI.GTextField", "UBBEnabled", true);
                        }
                    }
                }
                _rows.Add(cells);
            }
            _emptyText = FairyBridge.AddText(parent, "", 23, 0x70675B,
                55f, _rowsY + rowHeight * 3f, width - 110f, 55f);
            PrepareText(_emptyText);
            _pageLabel = FairyBridge.AddText(parent, "", 26, 0x282828,
                width / 2f - 56f, height - 76f, 112f, 52f);
            PrepareText(_pageLabel);
            _prevButton = FlatButton(parent, "‹", width / 2f - 132f,
                height - 78f, 55f, 54f, 0x333333);
            _nextButton = FlatButton(parent, "›", width / 2f + 77f,
                height - 78f, 55f, 54f, 0x333333);
            _closeButton = FlatButton(parent, "×", width - 72f, 8f,
                58f, 53f, 0x171717);
            int tabCount = detailTabs || characterTabs ? 4 : 0;
            if (tabCount > 0) {
                float tabWidth = detailTabs ? 248f : 184f;
                float gap = 14f;
                float tabsLeft = (width - (tabWidth * tabCount + gap * (tabCount - 1))) * 0.5f;
                for (int i = 0; i < tabCount; i++) {
                    IntPtr button = FlatButton(parent, "", tabsLeft + i * (tabWidth + gap),
                        79f, tabWidth, 48f, 0xDDDDDD);
                    _characterTabs.Add(button);
                    IntPtr caption = FairyBridge.AddText(button, "", 21, 0x202020,
                        6f, 0f, tabWidth - 12f, 48f);
                    PrepareText(caption);
                    _characterTabCaptions.Add(caption);
                }
            }
            SetHeaders(DefaultHeaders);
        }

        private static void PrepareText(IntPtr text) {
            FairyBridge.SetInt(text, "FairyGUI.GTextField", "autoSize", 0);
            FairyBridge.CenterText(text);
            FairyBridge.SetBool(text, "FairyGUI.GObject", "touchable", false);
        }

        private float RowHeight() {
            return Math.Min(47f, Math.Max(24f, (_height - _rowsY - 100f) / RowsPerPage));
        }

        private static void Rectangle(IntPtr parent, float x, float y,
            float width, float height, uint border, uint fill) {
            IntPtr graph = Il2CppAPI.CreateObject(FairyBridge.Class("FairyGUI.GGraph"));
            if (graph == IntPtr.Zero) {
                throw new InvalidOperationException("Could not allocate statistics background");
            }
            Il2CppReflection.InvokeMethod(FairyBridge.Method("FairyGUI.GGraph", ".ctor", 0),
                graph, Array.Empty<IntPtr>());
            IntPtr color = FairyBridge.Class("UnityEngine.Color");
            IntPtr[] args = {
                Il2CppReflection.BoxValue(FairyBridge.Class("System.Single"), width),
                Il2CppReflection.BoxValue(FairyBridge.Class("System.Single"), height),
                Il2CppReflection.BoxValue(FairyBridge.Class("System.Int32"), 1),
                Il2CppReflection.BoxValue(color, new NativeColor(border)),
                Il2CppReflection.BoxValue(color, new NativeColor(fill))
            };
            Il2CppReflection.InvokeMethod(FairyBridge.Method("FairyGUI.GGraph", "DrawRect", 5),
                graph, args);
            FairyBridge.SetBool(graph, "FairyGUI.GObject", "touchable", false);
            FairyBridge.SetXY(graph, x, y);
            FairyBridge.AddChild(parent, graph);
        }

        private static IntPtr TryPrefab(string package, string item) {
            IntPtr prefab = IntPtr.Zero;
            try {
                prefab = FairyBridge.PackageItem(package, item);
                if (prefab == IntPtr.Zero) {
                    return IntPtr.Zero;
                }
                // Buttons are created from initialized package items only;
                // no child handles are extracted from the native gacha history.
                return prefab;
            } catch (Exception) {
                if (prefab != IntPtr.Zero) {
                    try {
                        DisposePrefab(prefab);
                    } catch (Exception) {
                        // The resource may have been unloaded mid-construction.
                    }
                }
                return IntPtr.Zero;
            }
        }

        private static void DisposePrefab(IntPtr prefab) {
            Il2CppReflection.InvokeMethod(FairyBridge.Method("FairyGUI.GComponent",
                "Dispose", 0), prefab, Array.Empty<IntPtr>());
        }

        public static void ActivateNativeButton(IntPtr button) {
            FairyBridge.SetInt(button, "FairyGUI.GButton", "mode", 1);
            IntPtr field = FairyBridge.Field("FairyGUI.GButton", "changeStateOnClick");
            byte state = 1;
            SetNativeByte(button, field, ref state);
            FairyBridge.SetBool(button, "FairyGUI.GButton", "selected", false);
        }

        /// <summary>
        /// Bare GButton with a simple rectangular background. Does not inherit
        /// decorative circles, GearXY or transitions from AccountInfo buttons.
        /// </summary>
        public static IntPtr CreateEntryButton(IntPtr parent, string key,
            float width, float height, float x, float y, int fontSize = 34) {
            return FlatButton(parent, NativeSdkServices.Translate(key), x, y,
                width, height, 0xFF2498, fontSize);
        }

        private static IntPtr FlatButton(IntPtr parent, string glyph, float x, float y,
            float width, float height, uint background, int fontSize = 34) {
            // A manually constructed GButton runs only its constructor. The
            // game's GButton.ConstructExtension registers its private __click
            // handler, which updates `selected` in Check mode. Without that
            // handler our polling-based ConsumeClick() never sees a click.
            // 必须使用完成 ConstructExtension 的资源按钮，不能 new GButton 后只设 mode。
            IntPtr button = TryPrefab("Common", "Button_Next2");
            if (button == IntPtr.Zero) {
                button = TryPrefab("Common", "Button_Next");
            }
            if (button == IntPtr.Zero) {
                button = TryPrefab("AccountInfo", "Account_Button_Operator");
            }
            if (button == IntPtr.Zero) {
                button = TryPrefab("Setting", "Setting_Button_Operate");
            }
            if (button == IntPtr.Zero) {
                throw new InvalidOperationException(
                    "No initialized native button prefab available for statistics UI");
            }
            try {
                // Hide the source prefab's artwork, not its click listener.
                // The foreground rectangle and caption are completely owned by
                // this component, avoiding inherited arrows and tab circles.
                int count = FairyBridge.ChildCount(button);
                for (int index = 0; index < count; index++) {
                    IntPtr child = FairyBridge.GetChildAt(button, index);
                    FairyBridge.SetBool(child, "FairyGUI.GObject", "visible", false);
                    FairyBridge.SetBool(child, "FairyGUI.GObject", "touchable", false);
                }
                FairyBridge.SetSize(button, width, height);
                FairyBridge.SetXY(button, x, y);
                FairyBridge.SetBool(button, "FairyGUI.GComponent", "opaque", true);
                FairyBridge.SetBool(button, "FairyGUI.GObject", "touchable", true);
                FairyBridge.AddChild(parent, button);
                Rectangle(button, 0f, 0f, width, height, background, background);
                IntPtr caption = FairyBridge.AddText(button, glyph, fontSize,
                    background == 0xDDDDDD || background == 0xFF2498
                        ? 0x202020u : 0xFFFFFFu,
                    0f, 0f, width, height);
                PrepareText(caption);
                ActivateNativeButton(button);
                return button;
            } catch (Exception) {
                DisposePrefab(button);
                throw;
            }
        }

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "il2cpp_field_set_value")]
        private static extern void SetNativeByte(IntPtr instance, IntPtr field, ref byte value);

        private static float EstimateTextWidth(string value, float fontSize) {
            // A conservative width estimate for mixed CJK, numbers and UBB text.
            // Ignore rich-text tags; their rendered characters have zero width.
            float units = 0f;
            bool tag = false;
            foreach (char ch in value) {
                if (ch == '[') {
                    tag = true;
                    continue;
                }
                if (tag) {
                    if (ch == ']') tag = false;
                    continue;
                }
                units += ch == 'W' || ch == 'M' ? .9f : ch <= 0x7f ? .61f : 1.08f;
            }
            return units * fontSize + 26f;
        }

        private static readonly float[] CharacterMinimumWidths = {
            82f, 116f, 78f, 78f, 104f, 123f, 111f,
            295f, 95f, 310f, 108f, 123f, 82f
        };

        private static readonly float[] CharacterMaximumWidths = {
            230f, 240f, 125f, 125f, 160f, 195f, 160f,
            490f, 135f, 530f, 155f, 165f, 125f
        };

        /// <summary>
        /// Allocate measured data widths, protecting equipped-skin and combined
        /// friendship text before redistributing unused space. No font autoscale.
        /// Max widths prevent one malformed item name consuming the whole table.
        /// </summary>
        private static float[] CharacterWidths(IReadOnlyList<string> headers,
            IReadOnlyList<string[]>? data, float available) {
            int count = headers.Count;
            float[] desired = new float[count];
            float total = 0f;
            for (int i = 0; i < count; i++) {
                float measured = EstimateTextWidth(NativeSdkServices.Translate(headers[i]), 23f);
                if (data != null) {
                    foreach (string[] row in data) {
                        if (i < row.Length) {
                            measured = Math.Max(measured, EstimateTextWidth(row[i] ?? "", 22f));
                        }
                    }
                }
                desired[i] = Math.Max(CharacterMinimumWidths[i],
                    Math.Min(CharacterMaximumWidths[i], measured));
                total += desired[i];
            }
            if (total <= available) {
                // Extra space is distributed without reducing the text size.
                // Skin names and the two kinds of friendship XP get priority.
                float extra = available - total;
                for (int i = 0; i < count; i++) {
                    desired[i] += extra * (i == 7 ? .22f : i == 9 ? .24f : .54f / (count - 2));
                }
                return desired;
            }
            // Reduce *unused padding* on small columns first. Reserved minima
            // keep the meaningful data columns legible at the native font size.
            float deficit = total - available;
            for (int pass = 0; pass < 2 && deficit > .01f; pass++) {
                float reducible = 0f;
                for (int i = 0; i < count; i++) {
                    if (pass == 0 && (i == 7 || i == 9)) {
                        continue;
                    }
                    if (pass == 1 && i != 7 && i != 9) {
                        continue;
                    }
                    reducible += Math.Max(0f, desired[i] - CharacterMinimumWidths[i]);
                }
                if (reducible < .01f) continue;
                float fraction = Math.Min(1f, deficit / reducible);
                for (int i = 0; i < count; i++) {
                    if (pass == 0 && (i == 7 || i == 9)) {
                        continue;
                    }
                    if (pass == 1 && i != 7 && i != 9) {
                        continue;
                    }
                    float taken = Math.Max(0f, desired[i] - CharacterMinimumWidths[i]) * fraction;
                    desired[i] -= taken;
                    deficit -= taken;
                }
            }
            if (deficit > .01f) {
                // Extremely long localized data is not representable in 13
                // fixed columns at one font size. Report it rather than silently
                // shrinking the fonts; the full values remain in the data rows.
                NativeSdkServices.Warn("Character statistics width budget exceeded by " +
                    Math.Round(deficit) + " units; consider using a wider UI scale.");
            }
            return desired;
        }

        // Measure the complete data set, so paging does not move column boundaries.
        private float[] ContentWidths(IReadOnlyList<string> headers, IReadOnlyList<string[]>? data,
            float available, int preferred) {
            int count = headers.Count;
            float[] widths = new float[count], minima = new float[count];
            float total = 0f;
            for (int i = 0; i < count; i++) {
                minima[i] = i == preferred ? Math.Min(440f, available * .42f) : 82f;
                float measured = EstimateTextWidth(NativeSdkServices.Translate(headers[i]), 23f);
                if (data != null) foreach (string[] row in data)
                    if (i < row.Length) measured = Math.Max(measured, EstimateTextWidth(row[i] ?? "", 22f));
                widths[i] = Math.Max(minima[i], Math.Min(available * (i == preferred ? .65f : .4f), measured));
                total += widths[i];
            }
            if (total > available) {
                float reducible = 0f;
                for (int i = 0; i < count; i++) reducible += widths[i] - minima[i];
                float fraction = reducible > 0f ? Math.Min(1f, (total - available) / reducible) : 0f;
                for (int i = 0; i < count; i++) widths[i] -= (widths[i] - minima[i]) * fraction;
            } else {
                float extra = available - total;
                for (int i = 0; i < count; i++) widths[i] += extra *
                    (preferred < 0 ? 1f / count : i == preferred ? .75f : .25f / (count - 1));
            }
            return widths;
        }

        public void SetHeaders(IReadOnlyList<string> headers,
            IReadOnlyList<string[]>? data = null, int descriptionColumn = -1) {
            if (headers.Count < 2 || headers.Count > _maxColumns) {
                throw new ArgumentOutOfRangeException(nameof(headers),
                    "Invalid number of statistics table columns");
            }
            _columnCount = headers.Count;
            float available = _width - HorizontalPadding * 2f;
            float[] widths = new float[_columnCount];
            if (_wideCharacter && _columnCount == CharacterMinimumWidths.Length) {
                widths = CharacterWidths(headers, data, available);
            } else {
                widths = ContentWidths(headers, data, available, descriptionColumn);
            }
            float left = HorizontalPadding;
            for (int col = 0; col < _maxColumns; col++) {
                bool shown = col < _columnCount;
                FairyBridge.SetBool(_headers[col], "FairyGUI.GObject", "visible", shown);
                for (int row = 0; row < _rows.Count; row++) {
                    FairyBridge.SetBool(_rows[row][col], "FairyGUI.GObject", "visible", shown);
                }
                if (!shown) continue;
                float width = widths[col];
                FairyBridge.SetXY(_headers[col], left + 3f, _headerY);
                FairyBridge.SetSize(_headers[col], Math.Max(30f, width - 6f), HeaderHeight);
                FairyBridge.SetString(_headers[col], "FairyGUI.GTextField", "text",
                    NativeSdkServices.Translate(headers[col]));
                FairyBridge.SetString(_headers[col], "FairyGUI.GObject", "tooltips", headers[col] == "astralprobe.stats.mission_total" ? NativeSdkServices.Translate("astralprobe.stats.mission_total_hint") : NativeSdkServices.Translate(headers[col]));
                for (int row = 0; row < _rows.Count; row++) {
                    FairyBridge.SetXY(_rows[row][col], left + 3f,
                        _rowsY + row * RowHeight());
                    FairyBridge.SetSize(_rows[row][col], Math.Max(30f, width - 6f), RowHeight());
                }
                left += width;
            }
        }

        public IntPtr RowAction(int row) => row >= 0 && row < _rowActions.Count ? _rowActions[row] : IntPtr.Zero;
        public void SetRowActions(bool enabled, int column, int page, int total) {
            if (enabled && _rowActions.Count == 0)
                for (int i = 0; i < RowsPerPage; i++)
                    _rowActions.Add(CreateEntryButton(_parent, "astralprobe.stats.mission_details", 86f, RowHeight() - 5f, 0f, 0f, fontSize: 20));
            var bounds = enabled ? ColumnBounds(column) : (Left: 0f, Width: 0f);
            for (int i = 0; i < _rowActions.Count; i++) {
                FairyBridge.SetBool(_rowActions[i], "FairyGUI.GObject", "visible", enabled && page * RowsPerPage + i < total);
                if (!enabled) continue;
                FairyBridge.SetXY(_rowActions[i], bounds.Left + 6f, _rowsY + i * RowHeight() + 2f);
                FairyBridge.SetSize(_rowActions[i], Math.Max(78f, bounds.Width - 12f), RowHeight() - 5f);
            }
        }

        public void SetCaption(string text) => FairyBridge.SetString(_titleText, "FairyGUI.GTextField", "text", text);
        public (float Left, float Width) ColumnBounds(int column) => (
            FairyBridge.GetFloat(_headers[column], "FairyGUI.GObject", "x"),
            FairyBridge.GetFloat(_headers[column], "FairyGUI.GObject", "width"));

        public IntPtr DetailTab(int index) => CharacterTab(index);

        public void SetDetailTab(int selected) {
            SetModeCaptions(selected, new[] {
                "astralprobe.stats.detail.conditions",
                "astralprobe.stats.detail.missions",
                "astralprobe.stats.detail.weekly",
                "astralprobe.stats.detail.cosmetics"
            });
        }

        private void SetModeCaptions(int selected, IReadOnlyList<string> keys) {
            for (int i = 0; i < _characterTabs.Count && i < keys.Count; i++) {
                string text = NativeSdkServices.Translate(keys[i]);
                FairyBridge.SetString(_characterTabCaptions[i],
                    "FairyGUI.GTextField", "text",
                    i == selected ? "【" + text + "】" : text);
            }
        }

        public void SetCharacterTab(int selected) {
            // Kept as an SDK-compatible UI wheel; the character statistics view
            // itself now displays all data on one wide table without tabs.
            SetModeCaptions(selected, new[] {
                "astralprobe.stats.mode.matches", "astralprobe.stats.mode.pve",
                "astralprobe.stats.mode.bond", "astralprobe.stats.mode.collection"
            });
        }

        public void Render(IReadOnlyList<StatisticValue> values, int page) {
            var lines = new List<string[]>(values.Count);
            foreach (StatisticValue value in values) {
                lines.Add(new[] {
                    NativeSdkServices.Translate("astralprobe.stats.group." + value.Definition.Group),
                    NativeSdkServices.Translate(value.Definition.Key),
                    StatisticsPresentation.FormatCount(value.Value),
                    value.Definition.ConditionId.ToString(CultureInfo.InvariantCulture)
                });
            }
            RenderRows(lines, page);
        }

        public void RenderRows(IReadOnlyList<string[]> lines, int page,
            IReadOnlyList<bool>? contracted = null) {
            StatisticPageSlice slice = StatisticsPresentation.Slice(page, lines.Count, RowsPerPage);
            FairyBridge.SetBool(_emptyText, "FairyGUI.GObject", "visible", lines.Count == 0);
            FairyBridge.SetString(_emptyText, "FairyGUI.GTextField", "text", lines.Count == 0
                ? NativeSdkServices.Translate(_emptyKey) : "");
            for (int row = 0; row < _rows.Count; row++) {
                string[] columns = row < slice.Count ? lines[slice.Start + row] : Array.Empty<string>();
                for (int col = 0; col < _columnCount; col++) {
                    FairyBridge.SetString(_rows[row][col], "FairyGUI.GTextField", "text",
                        col < columns.Length ? columns[col] : "");
                    FairyBridge.SetString(_rows[row][col], "FairyGUI.GObject", "tooltips",
                        col < columns.Length ? columns[col] : "");
                    if (col == 1 && contracted != null) {
                        // Character names turn gold ONLY when the account has
                        // contracted this hero; reset recycled cells on every page.
                        int absoluteIndex = slice.Start + row;
                        bool golden = row < slice.Count && absoluteIndex < contracted.Count &&
                            contracted[absoluteIndex];
                        FairyBridge.SetColor(_rows[row][col], "FairyGUI.GTextField", "color",
                            golden ? 0xD9A62Bu : 0x463C35u);
                    }
                }
            }
            FairyBridge.SetString(_pageLabel, "FairyGUI.GTextField", "text",
                slice.DisplayPage.ToString(CultureInfo.InvariantCulture) + " / " +
                slice.PageCount.ToString(CultureInfo.InvariantCulture));
            FairyBridge.SetBool(_prevButton, "FairyGUI.GObject", "grayed", !slice.HasPrevious);
            FairyBridge.SetBool(_nextButton, "FairyGUI.GObject", "grayed", !slice.HasNext);
        }
    }
}
