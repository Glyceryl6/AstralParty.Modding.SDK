# AstralParty.Modding.SDK 0.5.0 — managed contracts / 托管 MOD 开发接口

`net6.0` library, no direct references to BepInEx, Unity, FairyGUI, IL2CPP or the game's hot-update DLLs.
The **host plugin** (`AstralProbe.dll`) owns every native object, checks the main thread and provides the runtime adapter.

## Capabilities / 主要接口

| API | What it does | Boundary |
| --- | --- | --- |
| `SdkHost.Capabilities` | Check hot-update type/method/field availability | Read-only; missing members return false |
| `SdkHost.ReadOwnStatistics()` | Read synchronized stats of the signed-in account | Never fakes missing server values |
| `SdkHost.Ui.RegisterSettingsTab()` | Create a MOD tab with native-looking groups, text, toggles and buttons | Called during plugin Load; rebuilt by host |
| `SdkHost.Ui.TryCreateWindow()` | Create a host-owned FairyGUI window with text, buttons and toggles | Requires native `Setting` assets loaded |
| `SdkHost.Ui.Watch()` | Observe a cached game window or child component (`UiWatchTarget`) | Polls in `HotfixWatcher.Update`; no hook patches |
| `SdkHost.ClientValues` | Get/temporarily override *registered client-only* values | Scoped, main-thread, restores only when unchanged |

All UI/data APIs are **main-thread-only**. Never call them from `Task.Run`, HTTP callbacks or file-watcher threads.
Never pass native pointers outside the host adapter, cache `GObject` across scenes, or use client overrides to fake server-authoritative resources.

### Add a settings tab / 注册原版风格选项卡

```csharp
using AstralParty.Modding.SDK;

bool enabled = false;
if (SdkHost.Ui is { } ui) {
    ui.RegisterSettingsTab("sample.status", "sample.tab.status", tab => {
        tab.AddGroup("sample.group.info", group => {
            group.AddText("This MOD runs on the host UI adapter");
            group.AddToggle("Enable", () => enabled, value => enabled = value);
            group.AddAction("Refresh", () => { /* game-thread callback */ });
            group.AddDynamicText(() => enabled ? "ON" : "OFF");
        });
    });
}
```

### Create a standalone native window / 创建独立窗口

```csharp
if (SdkHost.Ui?.TryCreateWindow(
        new ModWindowOptions("sample.help", "Help", 680, 460), out var window) == true &&
    window != null) {
    window.AddText("body", "Hello", 30, 85, 600, 60);
    window.AddButton("close", "Close", 460, 360, 160, 64, window.Close);
    window.Show();
    // Keep the managed window handle; call window.Dispose() before unloading.
}
```

Requires the game's Setting FairyGUI package. If that package is unavailable, `TryCreateWindow` returns false.
New windows are invalidated when their scene/root changes. Do not retain their handles indefinitely.

### Watch a window / 观察原版窗口或子组件

```csharp
IDisposable? subscription = SdkHost.Ui?.Watch(
    new UiWatchTarget("_cachedSettingInBattleWindow"),
    state => { if (state.Visible) { /* UI became visible */ } });

// For a named FairyGUI child chain:
// new UiWatchTarget("_cachedSettingInBattleWindow", "panelName/childName")
// Dispose on plugin unload:
subscription?.Dispose();
```

Name fields according to the **actual game build**; unknown fields safely disable the observer. The observer never opens the game's window or alters its events.

### Client-only data replacement / 客户端数据替换

```csharp
if (SdkHost.ClientValues?.TryOverride(
        "astralprobe.display.extended_statistics", false,
        out IDisposable? restore) == true)
{
    // Keep restore until your temporary UI action is finished.
    restore?.Dispose();
}
```

Registered slots are controlled by the host and are strictly local display/configuration values. A second override of the same slot is rejected until the first lease is disposed. An external update made during the lease is never overwritten by restoration.

## Development and deployment

```powershell
.\Diagnostics\Build-And-Stage-AstralProbe.ps1 `
    -ProjectDir $project -GameDir $game -Install
```

The script builds `AstralParty.Modding.SDK` first, builds the main project second, stages and hashes `AstralProbe.dll`, `AstralParty.Modding.SDK.dll`, `IL2CppSharp.dll`, then optionally installs all three with a rollback attempt.

Other plugins should reference ONE installed `AstralParty.Modding.SDK.dll`, not bundle private copies; the main host plugin must be loaded before accessing `SdkHost.Ui`.

SDK public APIs are not raw runtime patches. IL2CPP interoperability stays in `AstralProbe/Infrastructure/SdkUiHostAdapter.cs`, `FairyBridge.cs` and version-specific data adapters. See `docs/MOD-v0.27.0-SDK-HOST.md` for upgrade and compatibility notes.

## v0.3.0: Semantic condition IDs / 具名统计条件与格式化

Only the scalar achievement conditions consumed by AstralProbe are declared in
`StatisticConditionId` (for example `TotalDirectDamage = 3` and `TotalShopBuyCount = 311`).
Names and values are checked against this game's `ConditionType.cs`; they are
**read-only adapter keys**, not commands to send to the game server.
The presentation catalog still carries per-item localization and grouping metadata:

```csharp
using AstralParty.Modding.SDK;

int directDamage = (int)StatisticConditionId.TotalDirectDamage;
string formatted = StatisticsPresentation.FormatCount(12345); // "12345"
int pages = StatisticsPresentation.PageCount(32);             // 4 pages of 10
```

Version-sensitive IL2CPP pointers, UI child positions and game-specific package
resource strings intentionally remain in the host adapter, not the managed SDK.

## v0.4.0: Pure-managed utilities and game-data snapshots / 通用工具与热更数据

The **package version** and `SdkHost.ApiVersion` are now both `0.5.0`.
No game assemblies or native pointers have been added to the public API.

| API / file | Usage | Source and limits |
| --- | --- | --- |
| `StatisticsPresentation.Slice(page, count, size)` | Clamped zero-based `StatisticPageSlice` containing `Start`, `Count`, `DisplayPage`, `HasPrevious`, `HasNext` | Default page size 10 from `UI.GachaInfoWindow.recordMaxNum`; no integer overflow |
| `PngFiles.IsComplete(path)` / `HasCompleteTrailer(stream)` | Detect a written PNG signature + IEND ending | Only a lightweight completeness hint, **not** full PNG decoding / CRC validation |
| `ReplayArchivePolicy.VisibleSlots(requested, nativeCapacity)` | Preserve at least one free slot in the native replay list | Actual game limit is the *runtime* `StaticGlobalData.MAX_BATTLE_RECORDS`; default capacity 10 is a fallback, not a network constant |
| `GameMapModeId` | Map mode identifiers 0–12 | Compared with hot-update `MapModeType.cs` |
| `GameLanguageId` | Language identifiers 0, 10, 22, 23, 40, 41 | Compared with `LanguageType.cs` |
| `GameItemQualityId` | Item quality levels 0–5 | Compared with `QualityType.cs` |
| `GameGachaTableId` | Gacha pool kinds 0–4 | Compared with `GachaTableType.cs` |

These enum names are independent **read-only copies** of a known game build's values. They are not replacements for the original game's types, and must not be used to construct or forge protocol messages. Before relying on them after an update, re-check the generated hot-update enums.

### Example

```csharp
using AstralParty.Modding.SDK;

StatisticPageSlice page = StatisticsPresentation.Slice(page: 2, rowCount: 32);
// Page == 2, Start == 20, Count == 10, HasPrevious == true, HasNext == true
int visible = ReplayArchivePolicy.VisibleSlots(requested: 9, nativeCapacity: 10);
// visible == 9, one free native replay slot
bool saved = PngFiles.IsComplete(@"C:\\Screenshots\\capture.png");
```

`AstralProbe` now calls these SDK helpers instead of maintaining a duplicate
private `PngInspector` implementation and per-feature pagination/replay constants.

For a dependency-free smoke test on Windows with .NET 6 installed:

```powershell
dotnet run --project ../AstralParty.Modding.SDK.Tests/AstralParty.Modding.SDK.Tests.csproj -c Release
```

## v0.5.0: independent naming, indexed data, and game-style widgets

**Breaking rename:** the assembly, project folder and C# namespace are now
`AstralParty.Modding.SDK` instead of `AstralProbe.SDK`. Recompile dependent
plugins and place exactly one new `AstralParty.Modding.SDK.dll` beside
`AstralProbe.dll`. Do not silently delete the old DLL if another plugin uses it.
This is a shared interface library: the game-specific native adapter remains
in the host, so UI calls require the AstralProbe host and the live UI packages.
The package is not yet a self-sufficient replacement for that adapter.

### Reusable APIs

- `SdkHost.ReadOwnIndexedStatistics()` exposes character/maps counts from
  `TaskLogic.GetAchieveInfo_2`, with separate localized title and character
  name, and null for missing conditions. **No historical per-character kill,
  damage, or injury totals were found in the supplied account model.**
- `RouteReachability.Preview(origin, cameFrom, steps, forwardNeighbors)` returns
  bounded read-only possible routes, independent of Unity/IL2CPP. These do not
  represent a server-confirmed choice. `GameCharacterTypeId` and
  `GameLocalizationTableId` replace common numeric literals.
- `NativeUiCatalog.All` describes verified game assets: popup/window, tips,
  action buttons, checkboxes, input fields, the native volume slider, combobox,
  group box, scroll pane with native scroll bar, gacha history and popup footer.
  The descriptors are asset names **not game object pointers**.
- `SdkHost.Ui.TryShowToast(text, seconds)` uses the game's existing
  `SystemTipsWindow`; no new window manager is created.
- `IModWindow.AddCheckBox`, `AddGroupBox`, `AddInput`, `AddSlider`,
  `AddComboBox`, `AddScrollText` reuse the corresponding native components.
  All are host-owned and must only be called on Unity's main thread.
  `AddScrollText` retains the real `ScrollPane` and `GScrollBar` rather than
  mistaking the native scrollbar thumb for the entire control.

Example, using Java-style braces:

```csharp
using AstralParty.Modding.SDK;

if (SdkHost.Ui is { } ui && ui.TryCreateWindow(
    new ModWindowOptions("example.tools", "MOD Tools", 850, 600), out var window) &&
    window != null) {
    window.AddGroupBox("frame", "控件示例", 20, 60, 810, 490);
    window.AddInput("filter", "", 36, 130, 390, text => { /* state */ });
    window.AddSlider("volume", 0, 100, 50, 36, 210, 350, value => { /* state */ });
    window.AddComboBox("category", new[] { "A", "B" }, 0, 36, 280, 260,
        selected => { /* state */ });
    window.AddScrollText("help", "这里是可滚动的说明文字", 450, 130, 350, 260);
    window.AddCheckBox("enabled", "启用", 36, 355, () => false, value => { /* state */ });
    window.AddButton("close", "关闭", 620, 500, 160, 55, window.Close);
    window.Show();
}
```

If any native prefab has changed in the next game version, callers must
handle a missing asset / failed control creation. All widget-specific behaviors
still require in-game testing against the updated IL2CPP proxies.
