using System;
using System.Collections.Generic;

namespace AstralParty.Modding.SDK;

/// <summary>
/// Names of FairyGUI prefabs verified in generated UI source. These are asset
/// descriptors, NOT IL2CPP pointers; the host must check that each package is
/// already loaded, and may reject unsupported versions.
/// 原版资源目录只提供名称和用途；真实对象由宿主在主线程创建、验证并持有。
/// </summary>
public enum NativeWidgetKind {
    Window,
    Toast,
    ActionButton,
    CheckBox,
    Slider,
    Input,
    ComboBox,
    GroupBox,
    ScrollPane,
    ScrollBar,
    HistoryWindow,
    PopupFooter,
    ToggleGroup,
    ProfileActionButton,
    SettingsIconButton
}

public sealed record NativePrefabDescriptor(NativeWidgetKind Kind, string Package,
    string Item, string NativeType, string? EmbeddedControl = null);

public static class NativeUiCatalog {
    public static NativePrefabDescriptor Window { get; } =
        new(NativeWidgetKind.Window, "Setting", "Setting_Item_Buttom", "FairyGUI.GComponent");
    public static NativePrefabDescriptor Toast { get; } =
        new(NativeWidgetKind.Toast, "SystemTips", "SystemTipsWindow", "FairyGUI.GComponent");
    public static NativePrefabDescriptor ActionButton { get; } =
        new(NativeWidgetKind.ActionButton, "Setting", "Setting_Button_Operate", "FairyGUI.GButton");
    public static NativePrefabDescriptor CheckBox { get; } =
        new(NativeWidgetKind.CheckBox, "Setting", "Setting_Com_Toggle", "FairyGUI.GButton");
    public static NativePrefabDescriptor Input { get; } =
        new(NativeWidgetKind.Input, "Setting", "Setting_Com_InputField", "FairyGUI.GComponent",
            "UISetting_Com_InputField.title : FairyGUI.GTextInput");
    public static NativePrefabDescriptor Slider { get; } =
        new(NativeWidgetKind.Slider, "Setting", "Setting_Com_Volume", "FairyGUI.GComponent",
            "UISetting_Com_Volume.slider_MasterVolume : FairyGUI.GSlider");
    public static NativePrefabDescriptor ComboBox { get; } =
        new(NativeWidgetKind.ComboBox, "Setting", "Setting_Com_ComboBox", "FairyGUI.GComboBox");
    public static NativePrefabDescriptor GroupBox { get; } =
        new(NativeWidgetKind.GroupBox, "Setting", "Setting_Item_Buttom", "FairyGUI.GComponent");
    public static NativePrefabDescriptor ScrollPane { get; } =
        new(NativeWidgetKind.ScrollPane, "Setting", "Setting_Comp_Score_Desc", "FairyGUI.GComponent",
            "FairyGUI.GComponent.scrollPane");
    // Scrollbars are owned by ScrollPane; Setting_Grip alone is only the thumb.
    public static NativePrefabDescriptor ScrollBar { get; } =
        new(NativeWidgetKind.ScrollBar, "Setting", "Setting_Comp_Score_Desc", "FairyGUI.GComponent",
            "FairyGUI.ScrollPane.vtScrollBar : FairyGUI.GScrollBar");
    public static NativePrefabDescriptor HistoryWindow { get; } =
        new(NativeWidgetKind.HistoryWindow, "GachaInfo", "GachaInfoWindow", "FairyGUI.GComponent");
    public static NativePrefabDescriptor PopupFooter { get; } =
        new(NativeWidgetKind.PopupFooter, "Common", "Com_PopUpWindow_Bottom", "FairyGUI.GLabel");
    // Generated UISetting_Com_Camera exposes three check-button fields.
    public static NativePrefabDescriptor ToggleGroup { get; } =
        new(NativeWidgetKind.ToggleGroup, "Setting", "Setting_Com_Camera", "FairyGUI.GComponent",
            "UISetting_Com_Camera.com_KeyBoardSwither/com_MouseSwither/com_FreeCameraSwither");
    public static NativePrefabDescriptor ProfileActionButton { get; } =
        new(NativeWidgetKind.ProfileActionButton, "AccountInfo", "Account_Button_Operator",
            "FairyGUI.GButton", "UIAccount_Button_Operator.type");
    public static NativePrefabDescriptor SettingsIconButton { get; } =
        new(NativeWidgetKind.SettingsIconButton, "Setting", "Setting_Com_Button", "FairyGUI.GButton",
            "UISetting_Com_Button.info");

    public static IReadOnlyList<NativePrefabDescriptor> All { get; } = Array.AsReadOnly(new[] {
        Window, Toast, ActionButton, CheckBox, Slider, Input, ComboBox,
        GroupBox, ScrollPane, ScrollBar, HistoryWindow, PopupFooter,
        ToggleGroup, ProfileActionButton, SettingsIconButton
    });
}
