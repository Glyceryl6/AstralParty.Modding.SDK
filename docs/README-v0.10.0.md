# AstralParty.Modding.SDK v0.10.0 — Native 功能拆分与源码归档

## 目录

- `AstralParty.Modding.SDK.csproj`：纯托管 SDK（数据、配置、JSON 本地化、统计、UI 契约），**无需游戏或 Unity DLL**。
- `Collections`、`Configuration`、`Input`、`Interop`、`Localization`、`Data`、`Game`、`Runtime`、`Statistics`、`UI`：统一位于 SDK 根目录。
- `Native/AstralParty.Modding.SDK.Native.csproj`：独立的 IL2CPP/Unity 原生辅助程序集。
- `Native/Interop`（22 个类）、`Native/UI`（12 个类）：全部从 AstralProbe Framework 移入，命名空间分别为 `AstralParty.Modding.SDK.Native.Interop`、`AstralParty.Modding.SDK.Native.UI`。
- `Native/NativeSdkServices.cs`：宿主可选的翻译、日志、热更新布局值等回调；SDK 无对 `AstralProbe.Plugin`、`ModSettingsLayout` 的编译时引用。
- `Native/build/NativeInteropReferences.targets`：游戏代理引用的单一配置来源，MOD 通过 `Build/NativeInteropReferences.targets` 转发。
- `docs/`、`tests/`：旧的二次同名文件夹已移除，旧文档和测试工程仍保留。

## 构建

单独构建托管 SDK：

```powershell
dotnet build AstralParty.Modding.SDK.csproj -c Release
```

构建 Native SDK 前必须已运行过带 BepInEx IL2CPP 的游戏，并具备与游戏版本匹配的 `BepInEx/interop`。配置 `ASTRAL_PARTY_GAME_DIR` 环境变量，或复制 `Native/NativeLocal.props.example` 为 `Native/NativeLocal.props` 后填写游戏路径：

```powershell
dotnet restore Native/AstralParty.Modding.SDK.Native.csproj --configfile NuGet.Config
dotnet build Native/AstralParty.Modding.SDK.Native.csproj -c Release --no-restore
```

这部分使用与宿主一致的 `SNWCreations.Il2CppInterop` 版本、`IL2CppSharp` 和游戏代理。**不要**在游戏中覆盖已经修复 HybridCLR 的 BepInEx/Il2CppInterop DLL。

## 宿主初始化 / Host setup

```csharp
using AstralParty.Modding.SDK.Native;
using AstralParty.Modding.SDK.Native.Interop;
using AstralParty.Modding.SDK.Native.UI;

// 在 Unity 主线程的 BasePlugin.Load() 中调用，按需要传入回调。
NativeSdkServices.Configure(
    translate: key => MyLocalization.Translate(key),
    warn: text => Log.LogWarning(text));

// SDK Native 内的原生辅助方法仍需在 Unity 主线程调用。
NativeActionButton.StretchNativeArtwork(button, 400f, 60f);
```

已迁移的 UI 组件保留原有签名，提供与 AstralProbe 相同的默认布局参数；原 MOD 启动时注册其配置值提供器，以继续支持热更新。

**注意：** `NativeSdkServices` 当前是进程内共享的默认回调。多个独立 MOD 不应相互覆盖同一静态配置；不需要自定义标题和日志的功能可以保持默认。未来如需多宿主同时自定义服务，应增加显式的实例/作用域参数。该 SDK 不负责创建每个 MOD 的 BepInEx 插件入口。

## 部署 / Deployment

使用 SDK Native 的 MOD 需要随主 DLL 同目录部署 `AstralParty.Modding.SDK.dll` 和 `AstralParty.Modding.SDK.Native.dll`（或由统一依赖管理工具提供相同兼容版本）。本仓库中的 `AstralProbe/Diagnostics/Build-And-Stage-AstralProbe.ps1` 已包含四个程序集：主 MOD、托管 SDK、Native SDK、附属包。

该更新属于 **源代码命名空间变更**：原 `AstralProbe.Framework.Interop` / `.UI` 不再存在；依赖旧版命名空间的其他 MOD 必须重新编译。不要在项目中同时保留旧 Framework 类文件和新的 SDK Native 类。

## 验证 / Verification

SDK 工程 `tests/validate_sdk_v0100_migration.py` 进行目录、依赖隔离、命名空间、API 和项目引用检查。真正的 C# 编译与游戏加载仍需在具备匹配 .NET 6 SDK 和游戏互操作程序集的机器上执行。
