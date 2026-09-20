# 轻点 · Qingdian Auto Clicker

[![Windows CI](https://github.com/Liang5757/qingdian-auto-clicker/actions/workflows/ci.yml/badge.svg)](https://github.com/Liang5757/qingdian-auto-clicker/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

轻量、开源的 Windows 桌面连点器：**F9 开始，F10 停止**。中文界面，无网络请求、无遥测，无第三方运行时依赖包。

[English](README.en.md) · [下载发行版](https://github.com/Liang5757/qingdian-auto-clicker/releases/latest) · [报告问题](https://github.com/Liang5757/qingdian-auto-clicker/issues/new/choose)

![轻点主界面](docs/images/screenshot.png)

## 功能

| 配置 | 说明 |
| --- | --- |
| 点击间隔 | 20～3,600,000 毫秒，默认 100 毫秒 |
| 鼠标按键 | 左键、右键、中键 |
| 点击方式 | 单击或双击，每轮双击包含两次点击 |
| 点击轮数 | 设置完成轮数，0 表示持续运行 |
| 点击位置 | 跟随鼠标，或使用固定屏幕坐标；支持负坐标 |
| 延时取点 | 按下按钮后，在 3 秒内将鼠标移到目标位置 |
| 配置保存 | 启动连点、关闭窗口时保存，下次自动恢复 |

## 下载与使用

1. 从 [Releases](https://github.com/Liang5757/qingdian-auto-clicker/releases) 下载 `qingdian-auto-clicker-<版本>-windows.zip` 并解压。
2. 双击 `Qingdian.AutoClicker.exe`，设置点击方式和间隔。
3. 按 **F9**，在预留的 1 秒内将鼠标移到目标位置。
4. 按 **F10** 随时停止；关闭窗口也会停止。

支持 Windows 10/11，要求 .NET Framework 4.8 或更新的 4.x 版本。没有安装时请使用 [微软官方安装程序](https://dotnet.microsoft.com/download/dotnet-framework/net48)。无需安装开发用的 .NET SDK。发行包目前未做代码签名。

F9/F10 是全局快捷键，无需让窗口保持焦点。F10 注册失败时禁止启动；F9 被占用时可使用开始按钮，关闭冲突程序后重启即可重新注册。

配置文件位于 `%LOCALAPPDATA%\WindowsAutoClicker\settings.xml`，兼容初版工具路径。退出程序后删除该文件可重置配置。程序不添加开机启动项；卸载时删除解压目录即可，配置目录可另行删除。

## 已知限制

- 间隔由 Windows UI 定时器调度，不是实时精度保证；高负载下速度会变慢。
- 程序通过 Windows `SendInput` 发送点击，不能保证所有游戏、远程桌面或特殊窗口接受输入。
- 普通权限进程通常不能操作更高权限的目标；仅在需要时使用相同权限运行。
- 锁屏、安全桌面不支持输入；检测到输入失败或固定坐标失效时停止。
- 使用系统 DPI 感知模式；混合缩放多显示器可能出现界面模糊，请取点后验证目标位置。
- 自动化测试验证配置、计数、输入结构与消息处理，不替代真实桌面点击验收。首版尚未在完整 Windows/DPI/权限组合上人工验收，参见 [测试说明](docs/TESTING.md)。

请只在允许自动化操作的应用和场景中使用。

## 开发

要求 Windows、Git、.NET SDK 8.0.200 或更新的 8.0 SDK。SDK 版本范围由 `global.json` 固定；构建所需的 .NET Framework 引用程序集通过 NuGet 自动还原。

```powershell
git clone https://github.com/Liang5757/qingdian-auto-clicker.git
cd qingdian-auto-clicker
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build.ps1
.\src\Qingdian.AutoClicker\bin\Release\net48\Qingdian.AutoClicker.exe
```

构建脚本依次执行锁定依赖还原、Release 编译和 xUnit 测试，任何一步失败即停止。也可以在 Visual Studio 2022 中打开 `Qingdian.AutoClicker.sln`。

```powershell
# 生成便携 ZIP 及 SHA-256 校验文件
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/package.ps1
```

产物位于 `artifacts/`。不向源码仓库提交 EXE、bin、obj 或测试结果。依赖锁文件纳入版本控制。

## 项目结构

```text
src/Qingdian.AutoClicker/         WinForms 界面、原生输入、配置、会话计数
tests/Qingdian.AutoClicker.Tests/ xUnit 回归测试（不向桌面注入点击）
scripts/                        构建、测试、打包入口
docs/                           架构、测试、发布说明
.github/                        Windows CI、标签发布、协作模板
```

欢迎提交 Issue 和 Pull Request。请先阅读 [贡献指南](CONTRIBUTING.md)、[行为准则](CODE_OF_CONDUCT.md) 与 [安全政策](SECURITY.md)。

## 许可证

[MIT](LICENSE) © 2026 Qingdian contributors。
