import AppKit
import SwiftUI

final class AppDelegate: NSObject, NSApplicationDelegate, NSWindowDelegate, NSMenuDelegate {
    let model = AppModel()
    private var window: NSWindow!
    private var item: NSStatusItem!
    private var startItem: NSMenuItem!
    private var stopItem: NSMenuItem!
    func applicationDidFinishLaunching(_ notification: Notification) {
        if let bundle = Bundle.main.bundleIdentifier,
           let existing = NSRunningApplication.runningApplications(withBundleIdentifier: bundle).first(where: { $0.processIdentifier != ProcessInfo.processInfo.processIdentifier }) {
            existing.activate(options: [.activateIgnoringOtherApps]); NSApp.terminate(nil); return
        }
        buildMenus()
        window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 680, height: 740), styleMask: [.titled, .closable, .miniaturizable, .resizable], backing: .buffered, defer: false)
        window.title = "轻点 · macOS"; window.delegate = self; window.isReleasedWhenClosed = false
        window.contentView = NSHostingView(rootView: ContentView(model: model))
        window.center()
        model.stateChanged = { [weak self] in self?.updateMenu() }
        model.launch(); updateMenu()
        if model.settings.startHidden { NSApp.setActivationPolicy(.accessory) } else { showWindow() }
    }
    private func buildMenus() {
        let main = NSMenu()
        let root = NSMenuItem(); main.addItem(root); let appMenu = NSMenu(); root.submenu = appMenu
        let quit = NSMenuItem(title: "退出轻点", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        appMenu.addItem(quit)
        let edit = NSMenuItem(); edit.title = "编辑"; main.addItem(edit); let editMenu = NSMenu(title: "编辑"); edit.submenu = editMenu
        for (title, action, key) in [("剪切", "cut:", "x"), ("复制", "copy:", "c"), ("粘贴", "paste:", "v"), ("全选", "selectAll:", "a")] {
            editMenu.addItem(NSMenuItem(title: title, action: Selector(action), keyEquivalent: key))
        }
        NSApp.mainMenu = main
        item = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        item.button?.image = NSImage(systemSymbolName: "cursorarrow.click", accessibilityDescription: "轻点")
        let menu = NSMenu(); menu.autoenablesItems = false; menu.delegate = self
        let open = NSMenuItem(title: "打开轻点", action: #selector(showWindow), keyEquivalent: ""); open.target = self; menu.addItem(open)
        menu.addItem(.separator())
        startItem = NSMenuItem(title: "开始  F9", action: #selector(start), keyEquivalent: ""); startItem.target = self
        stopItem = NSMenuItem(title: "停止  F10 / Esc", action: #selector(stop), keyEquivalent: ""); stopItem.target = self
        menu.addItem(startItem); menu.addItem(stopItem); menu.addItem(.separator())
        let exit = NSMenuItem(title: "退出轻点", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q"); menu.addItem(exit)
        item.menu = menu
    }
    func menuWillOpen(_ menu: NSMenu) { model.refreshPermissions(); updateMenu() }
    private func updateMenu() {
        guard item != nil else { return }
        item.button?.title = model.running ? " 连点中" : ""
        item.button?.toolTip = model.running ? "轻点 · F10 / Esc 停止" : "轻点 · 已停止"
        startItem.isEnabled = !model.running && model.canStop && model.permission
        stopItem.isEnabled = model.running
    }
    @objc func showWindow() {
        guard window != nil else { return }
        NSApp.setActivationPolicy(.regular)
        window.deminiaturize(nil); window.makeKeyAndOrderFront(nil); NSApp.activate(ignoringOtherApps: true)
    }
    @objc private func start() { model.start() }
    @objc private func stop() { model.stop("已停止 · 菜单栏") }
    func windowShouldClose(_ sender: NSWindow) -> Bool {
        sender.orderOut(nil); NSApp.setActivationPolicy(.accessory); return false
    }
    func windowDidMiniaturize(_ notification: Notification) {
        window.orderOut(nil); NSApp.setActivationPolicy(.accessory)
    }
    func applicationShouldHandleReopen(_ sender: NSApplication, hasVisibleWindows flag: Bool) -> Bool { showWindow(); return true }
    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool { false }
    func applicationWillTerminate(_ notification: Notification) {
        model.shutdown(); if let item = item { NSStatusBar.system.removeStatusItem(item) }
    }
}

@main enum QingdianEntry {
    static func main() {
        let app = NSApplication.shared
        let delegate = AppDelegate()
        app.delegate = delegate; app.setActivationPolicy(.regular)
        withExtendedLifetime(delegate) { app.run() }
    }
}
