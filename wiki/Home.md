# AstralParty.Modding.SDK Wiki

适用 SDK v0.12.0，已把原 `Interop`、`UI` 及 `Native/Interop`、`Native/UI` 中的实现合并为 `AstralParty.Modding.SDK.Native`。项目仍包含托管统计与本地化 API，但统一 SDK 工程现在**需要目标游戏的 Unity/IL2CPP 代理程序集才能编译**。

- [01 快速开始](01-快速开始.md)
- [02 Native API 与示例](02-Native-API.md)
- [03 生命周期与故障排查](03-生命周期与故障.md)
- [04 从旧版本迁移](04-从旧版本迁移.md)
- [05 发布与维护 Wiki](05-发布与维护Wiki.md)

SDK 不是 Unity Editor 插件、不是游戏热更新 DLL，也不负责安装 BepInEx。需要完整从零指导请看 AstralProbe 项目的 Wiki。
