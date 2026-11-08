# AstralParty.Modding.SDK v0.10.1 — IL2CPP 引用修复

修复 SDK v0.10.0 中 **单独打开 SDK Native 工程无法继承 MOD 游戏路径**、只引用部分 `UnityEngine` 模块的问题。保留 SDK Core 为纯托管程序集，只有 `Native` 子工程引用游戏 IL2CPP DLL。

## 路径优先级

1. `-p:AstralInteropDir=...`（命令行全局属性），或环境变量 `ASTRAL_PARTY_INTEROP_DIR`；
2. 通过 `AstralGameDir`、`ASTRAL_PARTY_GAME_DIR` 计算 `<游戏目录>/BepInEx/interop`；
3. 单独打开 SDK 时，可以在 `Native/NativeLocal.props` 设置游戏目录；与 MOD 作为同级目录时，也可读取 `AstralProbe/AstralLocal.props`；
4. 如果以上都没有设置，则尝试 SDK Native / SDK 根目录中的 `lib/interop`（仅用于本地开发，不打包 DLL）。

SDK Native 会自动引用 `UnityEngine*.dll`、`Il2CppSystem*.dll` 和可用的 `Il2Cppmscorlib.dll`、`Unity.TextMeshPro.dll`、`Assembly-CSharp.dll`。这些引用均为 `Private=false`，不会作为 MOD 的分发文件复制。

## 单独在 Rider 中打开 SDK

将 `Native/NativeLocal.props.example` **复制**为 `Native/NativeLocal.props` 并设置真实游戏目录；该文件已列入 `.gitignore`。游戏目录必须已有 BepInEx 生成的 `interop` 代理 DLL，尤其是 `UnityEngine.CoreModule.dll`。如果目录不存在，需要先启动相匹配的 BepInEx IL2CPP 游戏以生成代理程序集。然后在 Rider 重新加载项目并还原 NuGet。

在项目根目录运行：

```powershell
# 替换成自己游戏的真实目录
$interop = 'D:\Games\Astral Party\BepInEx\interop'
dotnet restore .\Native\AstralParty.Modding.SDK.Native.csproj --configfile .\NuGet.Config
dotnet build .\Native\AstralParty.Modding.SDK.Native.csproj -c Release --no-restore -p:AstralInteropDir="$interop"
```

没有安装真实游戏互操作 DLL 时，IDE 或编译器仍会报 `UnityEngine` 缺失，这是预期的环境要求；不能使用空白 DLL 或游戏热更新源码代替它们。

此交付仅执行 XML/路径结构和静态回归检查；未在当前环境完成真实 .NET DLL 编译或游戏运行验证。
