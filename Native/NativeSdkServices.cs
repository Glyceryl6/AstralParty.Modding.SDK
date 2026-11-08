using System;

namespace AstralParty.Modding.SDK.Native;

/// <summary>
/// Optional host-supplied services for native UI helpers.
/// 原生 UI 通用组件通过回调使用宿主的翻译、日志与布局设置，不依赖具体 MOD。
/// Configure on the Unity plugin load thread before creating native controls.
/// </summary>
public static class NativeSdkServices {
    private static Func<string, string> _translate = static text => text;
    private static Action<string>? _warn;
    private static Action<string>? _info;
    private static Func<string>? _enumLabelAssemblyPath;
    private static Func<int>? _actionButtonFontSize;
    private static Func<float>? _actionButtonTextInset;
    private static Func<float>? _titleOutline;
    private static Func<bool>? _uiTypographyDiagnostics;

    public static void Configure(
        Func<string, string>? translate = null,
        Action<string>? warn = null,
        Action<string>? info = null,
        Func<string>? enumLabelAssemblyPath = null,
        Func<int>? actionButtonFontSize = null,
        Func<float>? actionButtonTextInset = null,
        Func<float>? titleOutline = null,
        Func<bool>? uiTypographyDiagnostics = null) {
        _translate = translate ?? (static text => text);
        _warn = warn;
        _info = info;
        _enumLabelAssemblyPath = enumLabelAssemblyPath;
        _actionButtonFontSize = actionButtonFontSize;
        _actionButtonTextInset = actionButtonTextInset;
        _titleOutline = titleOutline;
        _uiTypographyDiagnostics = uiTypographyDiagnostics;
    }

    public static string Translate(string text) => _translate(text);
    public static void Warn(string message) => _warn?.Invoke(message);
    public static void Info(string message) => _info?.Invoke(message);
    public static string EnumLabelAssemblyPath => _enumLabelAssemblyPath?.Invoke() ?? "";
    public static int ActionButtonFontSize => _actionButtonFontSize?.Invoke() ?? 38;
    public static float ActionButtonTextInset => _actionButtonTextInset?.Invoke() ?? 12f;
    public static float TitleOutline => _titleOutline?.Invoke() ?? 3f;
    public static bool UiTypographyDiagnostics => _uiTypographyDiagnostics?.Invoke() ?? false;
}
