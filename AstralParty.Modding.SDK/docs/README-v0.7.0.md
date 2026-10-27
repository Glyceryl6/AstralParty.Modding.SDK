# SDK 0.7.0

本次把通用服务与 AstralProbe 自身功能分开。SDK 仍为 net6.0 纯托管项目，不依赖 Unity、BepInEx、IL2CppInterop 或游戏热更新程序集；原生对象的创建、生命周期和游戏版本适配仍由宿主处理。

## 新增的通用能力

- `LocalizationCatalog`：每个 MOD 创建自己的实例，传入自己的默认文案。支持翻译回退、严格 JSON 检查、合并新默认键及保留用户翻译。文件无效时不覆盖用户文件；旧文案迁移由调用方提供回调。
- `ReloadableFile<T>`：读取后先解析、检查文件在读取期间是否变化，成功后才替换已接受的配置。失败时保留上次有效值；轮询频率、异常反馈和路径由调用方负责。
- `HotkeyChord`：只处理组合键文本和格式化。宿主提供主键名称的检查函数，因此其它引擎或 MOD 可以使用不同的保留键策略。
- `HotkeyRegistry` 与 `SdkHost.Hotkeys`：登记按键的 ID、显示名称、读取和写入回调，检测组合键冲突，提供可释放的登记对象。宿主设置页会在登记列表变化后重建。登记和释放应在 Unity 主线程执行；实际按键读取及配置保存由登记方处理。
- `NativeUiLayout.UniformScale`：按可用空间计算等比例缩放，保留资源宽高比例，不持有原生指针。

示例：在宿主就绪后登记自己的按键，并在卸载时释放登记。ID 使用 MOD 自己的命名空间；标签可以直接是已翻译文本。写入回调应保存自己 MOD 的配置。现有宿主仍保留 Steam F12、Esc 等录入限制。

```csharp
IDisposable registration = SdkHost.Hotkeys.Register(
    "example.mod.action", "我的功能",
    () => ownConfig.Value,
    value => { ownConfig.Value = value; ownConfig.Save(); });
// Unload: registration.Dispose();
```

不同 MOD 的语言文件和配置文件互相独立。SDK 不内置 AstralProbe 的中文、英文或旧文案迁移规则，也不会替其它 MOD 统一选择语言。

## 移回 MOD 的内容

`ReplayArchivePolicy` 和 `PngFiles` 已分别移回 AstralProbe 的回放与截图目录，取消通用 SDK 中的公开实现。`NativeUiCatalog.ReplayActionButton` 也已移除；通用按钮目录保留。

这是有意的公开 API 调整，因此升级至 0.7.0。依赖上述三项旧 API 的扩展需要重新编译，并将各自功能规则放到自己的 MOD 中。旧源码路径保留一行迁移说明，以便增量覆盖时清除原定义，不会生成旧类型。已不参与当前构建的历史 `AstralProbe.SDK` 项目保持原样，避免改动历史版本；当前 MOD 只引用 `AstralParty.Modding.SDK`。

## 验证

新增可执行的纯托管检查程序，覆盖语言实例隔离、无效文件保留、自定义文本保留、按键冲突、登记释放和按钮比例计算：

```text
dotnet run --project AstralParty.Modding.SDK/tests/CoreServices/CoreServices.csproj
```

本次环境没有 .NET SDK，未执行此 C# 程序，也未编译 SDK。已执行源码边界与原生契约检查，以及 C# 语法解析。发布前仍须在有 .NET SDK 的环境构建并运行上述检查。
