import AppKit
import ApplicationServices
import ServiceManagement
import Combine
import QingdianCore

final class AppModel: ObservableObject {
    @Published var settings = ClickSettings()
    @Published private(set) var running = false
    @Published private(set) var rounds = 0
    @Published private(set) var status = "就绪"
    @Published private(set) var permission = false
    @Published private(set) var canStop = false
    @Published private(set) var loginEnabled = false
    @Published private(set) var loginPending = false
    @Published var notice = "设置自动保存"
    var stateChanged: () -> Void = {}
    let keys = Hotkeys()
    private let mouse = MouseOutput()
    private lazy var engine = ClickEngine { [weak self] settings in self?.mouse.send(settings) ?? false }
    private let store: SettingsFile
    let log: LocalLog
    private var clickTimer: Timer?
    private var permissionTimer: Timer?
    private var workspaceObservers: [NSObjectProtocol] = []
    init(previewOnly: Bool = false) {
        let dir = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0].appendingPathComponent("Qingdian", isDirectory: true)
        store = SettingsFile(url: dir.appendingPathComponent("settings.json")); log = LocalLog(directory: dir)
        if previewOnly { permission = true; canStop = true; return }
        do { settings = try store.load() } catch { notice = "配置读取失败，使用默认值；修改设置后将重新保存。" }
    }
    func launch() {
        keys.onStart = { [weak self] in self?.start() }
        keys.onStop = { [weak self] in self?.stop("已停止 · F10 / Esc") }
        keys.install(); canStop = keys.canStop
        if !canStop { status = "F10 注册失败，禁止启动；关闭冲突软件后重开。" }
        else if !keys.canStart { status = "F9 被占用，请使用开始按钮。" }
        refreshPermissions()
        permissionTimer = Timer.scheduledTimer(withTimeInterval: 1, repeats: true) { [weak self] _ in self?.refreshPermissions() }
        if let timer = permissionTimer { RunLoop.main.add(timer, forMode: .common) }
        for name in [NSWorkspace.willSleepNotification, NSWorkspace.sessionDidResignActiveNotification, NSWorkspace.screensDidSleepNotification] {
            workspaceObservers.append(NSWorkspace.shared.notificationCenter.addObserver(forName: name, object: nil, queue: .main) { [weak self] _ in self?.stop("已停止 · 系统休眠或会话切换") })
        }
        log.write("application.started version=\(Bundle.main.infoDictionary?["CFBundleShortVersionString"] ?? "dev") stopHotkey=\(canStop)")
    }
    func save() {
        let normalized = settings.normalized
        do { try store.save(normalized); notice = "设置已自动保存" } catch { notice = "配置保存失败，本次设置仍可使用。" }
    }
    func refreshPermissions() {
        permission = AXIsProcessTrusted(); keys.refreshMonitors()
        if running && !permission { stop("已停止 · 辅助功能权限已失效") }
        loginEnabled = SMAppService.mainApp.status == .enabled
        loginPending = SMAppService.mainApp.status == .requiresApproval
    }
    func requestPermission() {
        let options = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true] as CFDictionary
        _ = AXIsProcessTrustedWithOptions(options)
        NSWorkspace.shared.open(URL(string: "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility")!)
    }
    func setLogin(_ enabled: Bool) {
        do {
            if enabled { try SMAppService.mainApp.register() } else { try SMAppService.mainApp.unregister() }
            refreshPermissions()
            notice = loginPending ? "请在系统设置的登录项中批准轻点。" : "登录启动设置已更新"
        } catch { notice = "无法更新登录项：\(error.localizedDescription)"; refreshPermissions() }
    }
    func start() {
        guard !running else { return }
        guard keys.canStop else { status = "F10 不可用，禁止启动"; return }
        guard AXIsProcessTrusted() else { status = "请先授权辅助功能权限"; permission = false; return }
        guard !keys.stopHeld else { status = "请先松开 F10 / Esc"; return }
        settings = settings.normalized
        save()
        let token = engine.start(settings)
        running = true; rounds = 0; status = "1 秒后开始 · F10 / Esc 停止"
        log.write("run.started interval=\(settings.interval) button=\(settings.button) double=\(settings.doubleClick) limit=\(settings.limit)")
        let interval = Double(settings.interval) / 1000
        let timer = Timer(fire: Date().addingTimeInterval(1), interval: interval, repeats: true) { [weak self] _ in
            guard let self = self else { return }
            if self.keys.stopHeld { self.stop("已停止 · 按键状态检测"); return }
            guard self.engine.tick(generation: token) else {
                if self.engine.failed { self.stop("点击发送失败，已停止") }
                return
            }
            self.rounds = self.engine.rounds
            self.status = "正在连点 · F10 / Esc 停止"
            if !self.engine.running { self.stop("已完成设定轮数") }
        }
        clickTimer = timer; RunLoop.main.add(timer, forMode: .common); stateChanged()
    }
    func stop(_ reason: String = "已停止") {
        engine.stop(); clickTimer?.invalidate(); clickTimer = nil; running = false; status = reason
        log.write("run.stopped rounds=\(rounds) reason=\(reason)"); stateChanged()
    }
    func shutdown() {
        stop("退出程序"); permissionTimer?.invalidate(); permissionTimer = nil; keys.uninstall()
        workspaceObservers.forEach { NSWorkspace.shared.notificationCenter.removeObserver($0) }; workspaceObservers.removeAll()
        log.write("application.exiting")
    }
    func openLog() { log.write("diagnostics.opened"); NSWorkspace.shared.open(log.url) }
}
