# Project instructions

- 保留“轻点”中文产品名称，默认使用中文文档；同步维护 README.en.md 的关键说明。
- Windows 使用 .NET Framework 4.8 / WinForms；macOS 使用 SwiftUI / AppKit 和 Swift Package Manager。两端不随意新增生产依赖。
- F9 启动、F10 停止是核心契约；F10 不可用时禁止启动。
- 不在自动测试中向真实桌面发送点击；原生输入变化需说明人工验证范围。
- 执行 scripts/build.ps1；改动后检查 diff，说明回归风险及实际验证结果。
- 不提交构建产物、用户配置、凭证或本地绝对路径。

- macOS 修改执行 `swift test` 与 `bash scripts/package-macos.sh`（需要 Mac，可使用 macOS CI）；保留 arm64/x86_64 通用包。
- 平台新增能力同步维护功能对照，不声称 Windows/macOS 已共享实现或自动化输入已实机验收。
