# 02 · `AstralParty.Modding.SDK.Native` 常用 API

SDK 不与游戏热更新源码直接编译耦合；通过原生元数据与 `IL2CppSharp` 操作运行中的 IL2CPP 对象。**方法名仅代表能力，不保证在任意游戏版本或任意线程安全。** 更详细约束见源码的 XML `<summary>` 注释。

| 目录 | 常用类 | 职责与注意点 |
|---|---|---|
| `Native/Runtime` | `Il2CppNative`, `NativeTypedFields`, `NativeValue`, `CheckedNativeCall` | 已解析的 IL2CPP 字段/方法、ABI 调用、类型大小检查。不能用任意 `IntPtr` 直接强转托管对象。|
| `Native/Runtime` | `NativeObjectLease`, `NativeReferenceFields`, `NativeGame` | 短期对象保活、字段操作、游戏管理器/窗口查询。游戏切换场景后应重新获取。|
| `Native/Assets` | `NativeAssetLookup`, `NativeCatalogLookup`, `InspectorEnumLabels` | 资源名与枚举映射，游戏更新后要重新验证元数据。|
| `Native/Controls` | `NativeActionButton`, `NativeToggleControl`, `NativeToggleLayout` | 文字按钮、选择控件及布局；需资源已加载且在 UI 主线程上运行。|
| `Native/Widgets` | `FairyBridge`, `NativeUiPackageLoader`, `NativeUiCatalog`, `NativeUiLayout` | FairyGUI 反射桥、资源探测、界面资源描述及托管尺寸计算。|
| `Native/Widgets` | `IModUiService`, `IModUiBuilder`, `IModWindow` | 由宿主提供的 UI 接口，插件不应持有原生窗口永久引用。|
| `Native` | `NativeSdkServices` | 宿主提供的翻译/日志/布局回调，**进程级共享**，不要让多个 MOD 反复覆写。|

## 示例 A：无游戏进程也可进行的排版计算

```csharp
using AstralParty.Modding.SDK.Native;

float s = NativeUiLayout.UniformScale(1280, 720, 640, 360);
float width = NativeUiLayout.EstimateCaptionWidth("开始", 28f);
```

## 示例 B：主线程检查原生 UI 是否可用

```csharp
using AstralParty.Modding.SDK.Native;

// 仅在 BepInEx 插件已有的 Unity 主线程生命周期回调中运行。
if (!FairyBridge.Ready() || !NativeGame.TryUiManager(out var ui))
    return;
// 使用 ui 立即操作/查询，不跨 Scene 长期缓存。
```

## 示例 C：短时保活和资源就绪

```csharp
using AstralParty.Modding.SDK.Native;

var package = new NativeUiPackageLoader("Setting");
if (package.Ready())
{
    // 资源已可查询，下一步依然需要检查具体控件是否存在。
}

// 对已确认有效的原生对象指针使用：
// using var lease = new NativeObjectLease(nativeObject);
```

`NativeActionButton.CreatePlain` 等方法可能依赖 FairyGUI 真实资源包和热更类型元数据。不要在 `Plugin.Load()` 时无条件创建，优先使用现有主线程 Update/窗口生命周期回调并处理失败路径。
