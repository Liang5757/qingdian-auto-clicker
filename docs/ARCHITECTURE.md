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

所有调度在 UI 线程执行：F9 注册启动，首个 tick 延迟一秒，后续使用设置的间隔。每轮点击将成对的 down/up 放入一次 SendInput 调用。只有完整发送的轮次才计数。双击作为一轮计数。

F10 停止定时器并结束会话；正在执行的一组 down/up 不会在中间中断。消息队列中的后续 tick 会检查运行状态。反复按 F9 不重置正在运行的会话。

v1.0.1 添加 `System.Threading.Timer` 每约 10 ms 读取 GetAsyncKeyState 的高位，后台线程只写停止标记，不操作界面。UI 使用独立的 20 ms 定时器处理该标记，每轮发送输入前也同步检查，避免依赖点击间隔或单一热键消息。已捕获的按下状态在松键后仍保留；开始新操作前显式重置，并重新检查按键是否仍按住。后台计时器随窗口释放。采样精度不受保证，桌面访问受限时 API 可返回零。

设置沿用初版 `%LOCALAPPDATA%/WindowsAutoClicker/settings.xml`。保存写入同目录唯一临时文件，再原子替换旧文件；失败时保留原文件，尽力清理临时文件。读取不允许 DTD，最多 64 Ki 字符。损坏配置不会在读取阶段被覆盖；用户启动或退出后会保存当前设置。

当前采用系统 DPI 感知而非 PerMonitorV2。未来若调整 DPI 策略，必须同步验证窗口缩放和跨显示器移动。
