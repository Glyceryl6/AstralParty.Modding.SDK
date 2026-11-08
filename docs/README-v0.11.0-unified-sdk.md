# AstralParty.Modding.SDK v0.11.0 — 原生组件合并到单个 SDK

本次将 SDK v0.10.1 的 `Native/Interop`（22 个类）和 `Native/UI`（12 个类）编入 SDK 根项目，保留原命名空间，不再创建 `AstralParty.Modding.SDK.Native.dll`。

## 工程结构

```text
AstralParty.Modding.SDK/
├── AstralParty.Modding.SDK.csproj    # 唯一 SDK 项目 / single SDK assembly
├── Native/                         # 源码目录，不含独立 csproj
│   ├── Interop/
│   ├── UI/
│   └── NativeSdkServices.cs
├── build/
│   ├── NativeInteropPaths.props
│   └── NativeInteropReferences.targets
├── SdkLocal.props.example           # 本机示例，复制为 SdkLocal.props
├── docs/
└── tests/
```

构建结果只有 `AstralParty.Modding.SDK.dll`。原来由两个程序集分别承载的类型现在均位于其中，原命名空间 `AstralParty.Modding.SDK.Native.*` **没有变化**。

## 使用与构建

因为 Unity/IL2CPP 原生辅助类已合并，**整个 SDK 现在依赖游戏代理程序集**，不再是可脱离游戏代理单独编译的纯托管库。这是减少 DLL 数量的取舍。

1. 先在本机安装并运行带 BepInEx IL2CPP 的对应游戏，让 `BepInEx/interop` 存在。
2. 复制 `SdkLocal.props.example` 为 `SdkLocal.props` 并填入游戏目录；也可复用相邻 `AstralProbe/AstralLocal.props`，或者传入 `-p:AstralInteropDir=...`。
3. 在 SDK 目录执行：

```powershell
dotnet restore .\AstralParty.Modding.SDK.csproj --configfile .\NuGet.Config
dotnet build .\AstralParty.Modding.SDK.csproj -c Release --no-restore
```

Rider 项目加载后若依旧标红，检查 `SdkLocal.props` 与游戏 `interop` 目录，再重新加载项目和还原包。旧版 `Native/NativeLocal.props` 在过渡期仍可读取；后续可迁移到 SDK 根目录。

## 升级与兼容

- 直接改用 `../AstralParty.Modding.SDK/AstralParty.Modding.SDK.csproj`，移除 `Native/AstralParty.Modding.SDK.Native.csproj` 项目引用。
- 旧 `Native/build/` 配置移动至根目录 `build/`，原 `Native.csproj` 已删除。
- 已编译、引用旧 `AstralParty.Modding.SDK.Native` 程序集的其他 MOD 必须重新编译；**仅改文件名不能迁移程序集标识**。
- 安装脚本只备份并移除 AstralProbe 自身目录中的旧 Native DLL，不动其他 MOD 的文件；使用者需确认其他 MOD 的依赖后手工清理。
- 不要将游戏代理 DLL 随 SDK 发布，避免和 BepInEx 生成版本冲突。

## 验证

`python tests/validate_sdk_v0110_unified.py` 可执行工程结构与引用检查。没有真实 .NET SDK 与游戏 IL2CPP 代理 DLL 的环境无法提供编译成功保证。
