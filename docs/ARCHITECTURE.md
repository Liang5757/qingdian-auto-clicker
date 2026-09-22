# 架构与设计

应用使用 SDK 风格 C# 项目，目标 .NET Framework 4.8，保持便携部署和较低的运行环境要求。构建使用 .NET 8 SDK；这是构建工具版本，与应用运行时不同。

| 模块 | 职责 |
| --- | --- |
| `Program` | 单实例互斥、DPI 和应用入口 |
| `MainForm` | 控件、全局快捷键、UI 定时器及状态显示 |
| `ClickSession` | 点击轮数、有限/无限模式、停止与重启行为 |
| `Native` | RegisterHotKey、SendInput、GetAsyncKeyState 的 Win32 封装 |
| `Settings` | 配置值与边界归一化 |
| `SettingsStore` | 受限 XML 读取、原子保存与失败回退 |
| `StopSignal` | 线程安全的 F10 停止请求；捕获后保持到显式重置 |

点击调度在 UI 线程执行：F9 注册启动，首个 tick 延迟一秒，后续使用设置的间隔。每轮点击将成对的 down/up 放入一次 SendInput 调用。只有完整发送的轮次才计数。双击作为一轮计数。

F10 停止定时器并结束会话；正在执行的一组 down/up 不会在中间中断。消息队列中的后续 tick 会检查运行状态。反复按 F9 不重置正在运行的会话。

v1.0.3 的 `InputMonitor` 在独立消息线程上安装只读低级键盘、鼠标钩子，并用 10 ms 定时器采样 F10 / Esc。键盘回调只设置 `StopSignal`，不操作 UI；鼠标回调只累计注入事件。停止标记保持到显式重置。

`ClickOutput` 串行化发送与停止，停止返回后后续 tick 无法继续发送。单次正在发送的 down/up 批次允许完成；停止不能撤销系统已接受的事件。部分发送失败时，仅在已接受前缀留下孤立 DOWN 时补发对应 UP，零事件失败不释放用户物理按住的键。

每个进程生成随机 `dwExtraInfo` 标记，`DiagnosticLog` 记录发送数量和钩子观测值；标记不是安全身份，可能被其他软件复制或移除。观测不用于阻拦其他软件输入，也不用于推断来源 PID。窗口释放时关闭输出、结束监测线程并卸载钩子。

设置沿用初版 `%LOCALAPPDATA%/WindowsAutoClicker/settings.xml`。保存写入同目录唯一临时文件，再原子替换旧文件；失败时保留原文件，尽力清理临时文件。读取不允许 DTD，最多 64 Ki 字符。损坏配置不会在读取阶段被覆盖；用户启动或退出后会保存当前设置。

当前采用系统 DPI 感知而非 PerMonitorV2。未来若调整 DPI 策略，必须同步验证窗口缩放和跨显示器移动。

## 界面与托盘（v1.1.0）

MainForm.Design 负责浅色布局与分段选择；MainForm.Tray 管理 NotifyIcon 和窗口生命周期。Hide 保留 HWND 和全局热键注册，UserClosing 默认取消并隐藏；明确退出、关机和系统退出路径仍停止输出并释放资源。开始菜单与按钮共同遵守 F10 可用性检查。托盘图标颜色反映运行状态。

StartupRegistration 只在用户切换复选框时更改 HKCU Run 项，路径整体加引号；读取配置和打开窗口不写注册表。StartHidden 保存在 XML 并兼容旧配置，默认 false。
