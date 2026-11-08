# 05 · 在 GitHub 上维护 SDK 文档

仓库根目录的 `README.md` 只负责 SDK 介绍和 Wiki 入口；详细编译步骤、Native API、生命周期和版本迁移写在本目录。这样版本更新时更容易审查文档与 API 是否一致。

## GitHub Wiki 实际发布

GitHub Wiki 是独立 `<OWNER>/<REPOSITORY>.wiki.git` 仓库，不会读取代码仓库的 `wiki/` 目录。先在 GitHub Wiki 网页中创建 `Home`，再克隆该 Wiki 仓库，将 `wiki/` 的 `.md` 页面复制到克隆仓库根目录；提交并推送后页面才会上线。参阅 [GitHub 文档](https://docs.github.com/en/communities/documenting-your-project-with-wikis/adding-or-editing-wiki-pages)。

## 文档验收建议

- 每项关键 API 的线程、指针所有权、回调及异常条件在源码 XML 注释和 Wiki 中一致。
- 版本更新时优先修改当前 Wiki，不在 README 中无限追加历史更新日志（发布说明归 Release）。
- 解释**真实 SDK 项目构建依赖**：统一 Native 后需要游戏 IL2CPP 代理程序集，不能当成纯托管库直接构建。
- 不上传本地 `SdkLocal.props`、游戏 DLL、原版 Unity 文件、BepInEx 修改二进制或用户日志。
- 对兼容性判断以目标游戏和真实 .NET/BepInEx 环境构建与加载为准。
