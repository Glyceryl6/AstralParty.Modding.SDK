# AstralParty.Modding.SDK

> **v0.12.0** · .NET 6 · 单一 `AstralParty.Modding.SDK.dll` · 面向《吉星派对》IL2CPP / HybridCLR MOD 开发。

本仓库提供用于多个 MOD 复用的工具：托管统计与集合/配置/本地化能力，以及基于 IL2CPP、FairyGUI 的原生访问、UI 绘制辅助。原有两套 `Interop` 和 `UI` 包现统一为 **`AstralParty.Modding.SDK.Native`** 命名空间，并按用途分放到 `Native/Runtime`、`Native/Assets`、`Native/Controls`、`Native/Widgets`、`Native/Gameplay`。SDK **只有一个工程、一个 DLL**。

## 从这里开始

- [SDK Wiki 首页](wiki/Home.md) · [快速接入和本机构建](wiki/01-快速开始.md)
- [Native API 分类与示例](wiki/02-Native-API.md)
- [原生对象生命周期与错误处理](wiki/03-生命周期与故障.md)
- [从旧版 SDK 迁移](wiki/04-从旧版本迁移.md)
- [如何发布 GitHub Wiki](wiki/05-发布与维护Wiki.md)
- [AstralProbe 主 MOD 开发指南](../AstralProbe/wiki/Home.md)

## 结构

```text
AstralParty.Modding.SDK/
├─ AstralParty.Modding.SDK.csproj
├─ Native/
│  ├─ Runtime/      # IL2CPP 元数据、字段、对象租约
│  ├─ Assets/       # 游戏目录、枚举与资源信息
│  ├─ Controls/     # FairyGUI 按钮、复选项和滚动列表
│  ├─ Widgets/      # FairyGUI 基础桥、窗口接口与尺寸计算
│  ├─ Gameplay/     # 少量游戏相关的只读判定辅助
│  └─ NativeSdkServices.cs
├─ Collections/ Configuration/ Localization/ Input/
├─ Data/ Game/ Navigation/ Runtime/ Statistics/
├─ build/            # UnityEngine / Il2Cppmscorlib 的路径与引用
├─ tests/
└─ wiki/
```

**SDK v0.12.0 是源码兼容性更新。** `using AstralParty.Modding.SDK.Native.Interop;`、`.Native.UI;` 和原位于 SDK 根命名空间的 `Il2CppNative`、`IModUiService` 等类型，需改为 `using AstralParty.Modding.SDK.Native;` 并重新编译。SDK v0.11.0 后合并了 Native DLL，已经引用旧程序集的 MOD 也必须重新编译。

## 构建说明

需要当前游戏生成的 `BepInEx/interop` 代理 DLL（Unity 模块、`Il2Cppmscorlib.dll` 等）；并列放置 `AstralProbe/` 可继承其本机配置。SDK 独立打开时复制 `SdkLocal.props.example` 为 `SdkLocal.props`，填写真实游戏目录，**不要提交本机路径文件**。

```powershell
dotnet restore .\AstralParty.Modding.SDK.csproj
dotnet build .\AstralParty.Modding.SDK.csproj -c Release `
  -p:AstralInteropDir='D:\Games\Astral Party\BepInEx\interop'
```

SDK 使用 `SNWCreations.Il2CppInterop.Runtime/Common` 与 `IL2CppSharp` 相关 NuGet 包；与 BepInEx 的运行时程序集匹配要求参看 [主 MOD 的定制 Il2CppInterop 指南](../AstralProbe/wiki/03-定制Il2CppInterop.md)。仅有 .NET SDK 或只有游戏热更新源码，不能保证完成构建。

**安全与分发：** SDK 源码不包含游戏资产，生成的代理 DLL 与 BepInEx 核心库不可随意再分发。原生辅助类必须从 Unity 主线程调用，不要跨场景持有 `IntPtr`。`NativeSdkServices` 的回调是进程级静态配置，尚未按 MOD ID 隔离。
