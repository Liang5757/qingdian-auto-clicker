# 轻点 · macOS

原生 SwiftUI / AppKit 版本，要求 **macOS 13 或更新版本**。发行包包含 Intel（x86_64）与 Apple Silicon（arm64）通用二进制，无第三方运行时包。

## 安装与操作

1. 解压 `qingdian-auto-clicker-<版本>-macos-universal.zip`，将 `轻点.app` 移到“应用程序”文件夹再打开。
2. 使用界面的“前往授权”，在系统设置 → 隐私与安全性 → 辅助功能中允许轻点。授权后界面自动刷新；若系统未刷新权限，请退出并重开。
3. 设置间隔（20 ms～1 小时）、左/右/中键、单击/双击和轮数。0 轮表示持续运行。
4. F9 开始，预留 1 秒移动鼠标；F10 停止，Esc 为备用停止。F10 注册失败禁止启动。
5. Mac 键盘的顶排可能默认控制媒体或音量，请同时按 Fn / 🌐，或在系统键盘设置中把 F1～F12 用作标准功能键。
6. 关闭或最小化窗口后驻留菜单栏，当前会话继续。菜单栏可打开、开始、停止或退出；退出停止输出。

后台驻留依旧点击鼠标当前位置，不提供对最小化目标窗口的后台点击。启动时隐藏和登录时启动均默认关闭；登录启动只启动应用，不自动连点。登录项由系统 ServiceManagement 管理，可能需要在系统设置中批准；失败会显示错误。卸载前请取消登录启动并退出，再删除应用。

## 安全与隐私

应用不联网、不上传遥测。设置和日志位于 `~/Library/Application Support/Qingdian/`；配置为 `settings.json`，诊断日志约最多 1 MB。不记录鼠标坐标、文本内容或其他应用窗口标题。

当前下载包仅做 **ad-hoc 签名**，未使用 Apple Developer ID，也未完成公证。它不能证明开发者身份，macOS 可能阻止首次启动。核验来源后按系统“隐私与安全性”中的打开提示操作，或从源码构建；无需关闭 Gatekeeper/SIP。本项目不提供绕过系统权限的功能。更新或移动应用后可能需要重新授权辅助功能。

CGEvent.post 没有目标接收确认，界面的轮数是完整事件组的提交次数，不保证目标软件已响应。模拟点击不支持所有游戏或受保护窗口；锁屏、安全输入模式、系统快捷键拦截等情况不保证快捷键生效。休眠、屏幕休眠、会话切换会请求停止且不会自动恢复；锁屏前仍应手动停止。

## 开发与测试

安装 Xcode 命令行工具和支持 Swift 5.9 的工具链：

```bash
cd macos
swift test -c release
swift run Qingdian
# 在仓库根目录打包
cd ..
bash scripts/package-macos.sh
```

脚本执行纯逻辑测试、分别编译两个架构、合并通用二进制、生成图标、验证结构签名、打 ZIP 与 SHA-256。运行 `swift run` 时权限归属可能与打包后的应用不同，应优先用打包 app 做权限验收。

CI 不会向桌面注入点击，也不授予辅助功能权限。编译、单元测试和结构签名成功不代表真实键盘、鼠标、登录项和多屏 UI 已验收。请在无副作用点击计数窗口检查三种按键、双击、20 ms/长间隔、启动延迟停止、隐藏后停止和退出。

## 设计

`QingdianCore`：配置、计数、发送替身接口、会话代次；过期回调不能在重启后继续发送。

`Qingdian`：SwiftUI 界面、AppKit 菜单栏、Carbon 全局快捷键、NSEvent 停止监听、CGEvent 输出、系统权限和登录项。

两端属于同一个项目，共享产品契约、版本和发布流程；Windows 的 C# 与 macOS 的 Swift 实现分别维护，目前不支持 Linux。

参考：[辅助功能权限](https://developer.apple.com/documentation/applicationservices/1459186-axisprocesstrustedwithoptions)、[键盘事件监听](https://developer.apple.com/library/archive/documentation/Cocoa/Conceptual/EventOverview/MonitoringEvents/MonitoringEvents.html)、[登录项 API](https://developer.apple.com/documentation/servicemanagement/smappservice/register())、[通用二进制](https://developer.apple.com/documentation/apple-silicon/building-a-universal-macos-binary)。
