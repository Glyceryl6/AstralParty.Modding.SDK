using System;
using System.Collections.Generic;

namespace AstralParty.Modding.SDK;

/// <summary>
/// Stable UI contract. The host owns all native FairyGUI objects; SDK consumers
/// never receive native pointers or references to the game's hot-update types.
/// 稳定 UI 接口：游戏资源和 IL2CPP 对象由宿主拥有，第三方插件只操作托管句柄。
/// </summary>
public interface IModUiService {
    /// <summary>Register a page in AstralProbe settings; callbacks run on Unity's main thread.</summary>
    bool RegisterSettingsTab(string id, string localizedTitle, Action<IModUiBuilder> populate);

    /// <summary>Create a standalone, host-owned FairyGUI window; false if the UI is not ready.</summary>
    bool TryCreateWindow(ModWindowOptions options, out IModWindow? window);

    /// <summary>
    /// Subscribe to a game's cached window, optionally watching a nested child by name.
    /// Returns a disposable subscription; no native reference is retained between frames.
    /// 监听游戏原生窗口及子组件；每帧重新获取引用，避免跨场景缓存悬空指针。
    /// </summary>
    IDisposable Watch(UiWatchTarget target, Action<UiWatchState> onChanged);

    /// <summary>Invoke the existing native SystemTipsWindow; never create a UIManager.</summary>
    bool TryShowToast(string message, float durationSeconds = 3f);
}

/// <summary>Safe extension page writer / 可组合的扩展页 UI 构建器。</summary>
public interface IModUiBuilder {
    void AddGroup(string title, Action<IModUiBuilder> populate);
    void AddText(string text);
    void AddToggle(string label, Func<bool> read, Action<bool> write);
    void AddAction(string label, Action onClick);
    void AddDynamicText(Func<string> read);
}

/// <summary>Dimensions are FairyGUI logical units, not display pixels.</summary>
public sealed record ModWindowOptions(string Id, string Title, float Width = 680f,
    float Height = 460f, bool Modal = true, bool CloseOnSceneChange = true);

/// <summary>Text-only native window controls; all methods must run on Unity's main thread.</summary>
public interface IModWindow : IDisposable {
    string Id { get; }
    bool IsOpen { get; }
    void Show();
    void Close();
    void AddText(string id, string text, float x, float y, float width, float height, int fontSize = 24);
    void AddButton(string id, string title, float x, float y, float width, float height, Action onClick);
    void AddToggle(string id, string label, float x, float y, Func<bool> read, Action<bool> write);
    /// <summary>Alias for the game's selected-state check button.</summary>
    void AddCheckBox(string id, string label, float x, float y, Func<bool> read, Action<bool> write);
    void AddGroupBox(string id, string title, float x, float y, float width, float height);
    void AddInput(string id, string initial, float x, float y, float width, Action<string> onChanged);
    void AddSlider(string id, double minimum, double maximum, double initial,
        float x, float y, float width, Action<double> onChanged);
    void AddComboBox(string id, IReadOnlyList<string> items, int initialIndex,
        float x, float y, float width, Action<int> onChanged);
    void AddScrollText(string id, string text, float x, float y, float width, float height);
    bool TrySetText(string id, string text);
}

/// <summary>
/// CachedWindowField is a UI.UIManager field (e.g. _cachedSettingWindow).
/// ChildPath is a slash-separated chain of FairyGUI GComponent child names.
/// Only observation is supported: this API never opens/closes game-owned windows.
/// </summary>
public sealed record UiWatchTarget(string CachedWindowField, string? ChildPath = null);

public sealed record UiWatchState(bool Exists, bool Visible, bool Enabled, bool Selected,
    string Text = "");
