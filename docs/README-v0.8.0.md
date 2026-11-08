# SDK 0.8.0

此版继续把真正通用的实现放入 SDK，把 AstralProbe 的特定功能规则留在 MOD。

## 共用原生方法

新增可选的 `Il2CppNative`：

- `ObjectLease` 保持实际 IL2CPP 对象存活，释放操作可重复调用。
- `ReadString`、`NewString` 读写原生字符串；读取时检查长度上限。
- `ReadValue<T>`、`WriteValue<T>` 操作值字段，检查对象所属类型、字段类别和实际值宽度，引用字段不能作为普通值字段写入。
- `SetReference` 使用专用引用写入 API，检查所属类型、引用类型兼容性和写入结果，并保护两个对象的生存期。
- `InvokeInstance` 检查实例类型，保护生存期，调用后报告原生异常。
- `Unbox<T>` 只接受大小匹配的值类型。

这些是低层方法，只能传入当前运行时真实有效的对象和元数据指针，在游戏允许的线程上调用。大小检查不等于语义类型检查：例如同宽的整数、枚举和浮点数仍须由调用方正确选择。实例方法的参数必须严格遵循 IL2CPP 的引用/值参数规则，SDK 不替调用方猜测重载或签名。

本项目不增加 Unity、BepInEx、SNW 或游戏程序集的编译引用，也不增加额外运行库文件；但新增模块包含 P/Invoke，使用时需要实际 GameAssembly。不能把“没有额外程序集依赖”理解为“原生方法可以离线运行”。其余配置、本地化、按键等托管能力仍可独立使用。

MOD 的 `NativeValue`、`NativeObjectLease`、`NativeReferenceFields`、`CheckedNativeCall`、`NativePreviewList` 和 `NativeFairyText` 已改用这些共用方法，保留游戏版本相关的字段及方法查找。底层入口名与参数形式已对照 Il2CppInterop 的公开声明和既有工程调用；安装版本的实际导出、HybridCLR 元数据与 GC 句柄布局仍需实机验证。

## 共用集合方法与移出的规则

新增 `StableMerge.AppendSafe`：保留原始顺序，按调用方给定的键和顺序追加符合条件的去重项，不修改原集合。

原 `HeroPreviewOrdering` 的游戏规则移到 MOD 角色预览目录，SDK 中不再有该公开类。原 `AchievementPresentation` 及其相关显示记录类型移到 MOD 统计读取目录，成就标签和语言相关的描述处理也不再放在 SDK。

这是公开 API 调整，因此升级至 0.8.0。使用上述旧类型的扩展需要调整并重新编译。旧源码路径只保留迁移说明，以便增量覆盖时清除旧定义；其它通用数据和统计契约保留。

## 检查

`tests/CoreServices` 增加稳定合并的检查，原有本地化、配置和按键检查保留。该程序不调用新增原生方法。

```text
dotnet run --project AstralParty.Modding.SDK/tests/CoreServices/CoreServices.csproj
```

本次环境没有 .NET SDK，未编译或执行这个 C# 程序。已执行语法解析与源码契约检查。原生方法必须在游戏中另行验证，不能用纯托管检查结果替代。
