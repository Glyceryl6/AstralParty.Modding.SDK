# SDK v0.12.0 · Native 包归并与文档整理

此次为**源码不兼容**的目录／命名空间调整，基于 SDK v0.11.0。

- 移除 4 个只有说明性注释、没有实际类型声明的 `.cs` 占位文件：`Game/HeroPreviewOrdering.cs`、`Media/PngFiles.cs`、`Replay/ReplayArchivePolicy.cs`、`Statistics/AchievementPresentation.cs`。
- 将原 `Interop/`、`UI/`、`Native/Interop/`、`Native/UI/` 的共 40 个实现文件统一整理到 `Native/Runtime`、`Native/Assets`、`Native/Controls`、`Native/Widgets`、`Native/Gameplay`。
- 将迁移代码的命名空间统一为 `AstralParty.Modding.SDK.Native`，保留原始对外类型名称；若 C# 文件仍通过 `using ...SDK.Native.Interop`、`using ...SDK.Native.UI` 或 `using ...SDK.Interop` 引用，必须改为 `using AstralParty.Modding.SDK.Native;`，并重新编译所有依赖方。
- 仍只构建一个 `AstralParty.Modding.SDK.dll`。**不**恢复独立 Native 项目，也不加入原版热更新程序集作为常规项目引用。
- 补充了常用原生 ABI、FairyGUI、按钮、对象生命周期等 API 的 XML 注释和 `README.md`／`wiki/` 开发指南。

## 更新方式

- **Git 用户**：推荐应用 Git 补丁（依次 `git am`），文件移动与删除由 Git 处理。
- **增量源码用户**：先备份修改，再复制增量包新增／修改文件，最后可以先运行 `scripts/Remove-OldNativeSources-v0.12.0.ps1` 预览，再运行带 `-Apply` 的命令备份并迁出原路径（仅自动处理未改动的旧文件）；也可以依据增量包内的 `删除与移动清单.txt` 手动清理；否则 SDK 默认递归编译会把旧、新两份类都纳入，导致重复类型错误。不要删除自己曾经改写而尚未备份的旧文件。

完成后使用相邻的 AstralProbe v0.50.0 或自行迁移的插件工程，在安装真实游戏互操作库的本机运行 `dotnet restore`、`dotnet build`，并做游戏内验证。仓库静态检查不能保证全部 IL2CPP 调用在当前游戏版本安全。

进一步阅读：[GitHub Wiki 首页](../wiki/Home.md)、[Native API](../wiki/02-Native-API.md)、[迁移指南](../wiki/04-从旧版本迁移.md)。
