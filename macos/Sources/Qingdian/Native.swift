import AppKit
import ApplicationServices
import Carbon
import QingdianCore

final class MouseOutput {
    private let marker = Int64.random(in: 1...Int64.max)
    func send(_ settings: ClickSettings) -> Bool {
        guard AXIsProcessTrusted(), let position = CGEvent(source: nil)?.location else { return false }
        let buttons: [CGMouseButton] = [.left, .right, .center]
        let downs: [CGEventType] = [.leftMouseDown, .rightMouseDown, .otherMouseDown]
        let ups: [CGEventType] = [.leftMouseUp, .rightMouseUp, .otherMouseUp]
        let i = settings.button
        var events: [CGEvent] = []
        for click in 1...(settings.doubleClick ? 2 : 1) {
            guard let down = CGEvent(mouseEventSource: nil, mouseType: downs[i], mouseCursorPosition: position, mouseButton: buttons[i]),
                  let up = CGEvent(mouseEventSource: nil, mouseType: ups[i], mouseCursorPosition: position, mouseButton: buttons[i]) else { return false }
            for event in [down, up] {
                event.setIntegerValueField(.mouseEventClickState, value: Int64(click))
                event.setIntegerValueField(.eventSourceUserData, value: marker)
                events.append(event)
            }
        }
        // Construct the entire batch first; never issue DOWN if its UP cannot be created.
        // CGEvent.post has no delivery acknowledgement; true means submitted, not received.
        events.forEach { $0.post(tap: .cghidEventTap) }
        return true
    }
}

final class Hotkeys {
    private var handler: EventHandlerRef?
    private var startRef: EventHotKeyRef?
    private var stopRef: EventHotKeyRef?
    private var globalMonitor: Any?
    private var localMonitor: Any?
    private(set) var canStop = false
    private(set) var canStart = false
    var onStart: () -> Void = {}
    var onStop: () -> Void = {}
    func install() {
        var spec = EventTypeSpec(eventClass: OSType(kEventClassKeyboard), eventKind: UInt32(kEventHotKeyPressed))
        let result = InstallEventHandler(GetApplicationEventTarget(), { _, event, context in
            guard let event = event, let context = context else { return OSStatus(eventNotHandledErr) }
            var id = EventHotKeyID()
            let result = GetEventParameter(event, EventParamName(kEventParamDirectObject), EventParamType(typeEventHotKeyID), nil,
                                           MemoryLayout<EventHotKeyID>.size, nil, &id)
            guard result == noErr else { return result }
            let owner = Unmanaged<Hotkeys>.fromOpaque(context).takeUnretainedValue()
            if id.id == 10 { owner.onStop() }
            if id.id == 9 { owner.onStart() }
            return noErr
        }, 1, &spec, Unmanaged.passUnretained(self).toOpaque(), &handler)
        guard result == noErr else { return }
        canStop = RegisterEventHotKey(UInt32(kVK_F10), 0, EventHotKeyID(signature: 0x5144494E, id: 10), GetApplicationEventTarget(), 0, &stopRef) == noErr
        if canStop {
            canStart = RegisterEventHotKey(UInt32(kVK_F9), 0, EventHotKeyID(signature: 0x5144494E, id: 9), GetApplicationEventTarget(), 0, &startRef) == noErr
        }
        refreshMonitors()
    }
    func refreshMonitors() {
        if localMonitor == nil {
            localMonitor = NSEvent.addLocalMonitorForEvents(matching: .keyDown) { [weak self] event in
                if event.keyCode == UInt16(kVK_Escape) || event.keyCode == UInt16(kVK_F10) { self?.onStop() }
                return event
            }
        }
        if globalMonitor == nil && AXIsProcessTrusted() {
            globalMonitor = NSEvent.addGlobalMonitorForEvents(matching: .keyDown) { [weak self] event in
                if event.keyCode == UInt16(kVK_Escape) || event.keyCode == UInt16(kVK_F10) { self?.onStop() }
            }
        }
    }
    func uninstall() {
        if let ref = startRef { UnregisterEventHotKey(ref) }; startRef = nil
        if let ref = stopRef { UnregisterEventHotKey(ref) }; stopRef = nil
        if let ref = handler { RemoveEventHandler(ref) }; handler = nil
        if let monitor = localMonitor { NSEvent.removeMonitor(monitor) }; localMonitor = nil
        if let monitor = globalMonitor { NSEvent.removeMonitor(monitor) }; globalMonitor = nil
        canStop = false; canStart = false
    }
}

final class LocalLog {
    let url: URL
    init(directory: URL) { url = directory.appendingPathComponent("diagnostics.log") }
    func write(_ text: String) {
        do {
            try FileManager.default.createDirectory(at: url.deletingLastPathComponent(), withIntermediateDirectories: true)
            let size = (try? url.resourceValues(forKeys: [.fileSizeKey]).fileSize) ?? 0
            if size > 524_288 {
                let old = url.appendingPathExtension("1")
                if FileManager.default.fileExists(atPath: old.path) { try FileManager.default.removeItem(at: old) }
                try FileManager.default.moveItem(at: url, to: old)
            }
            if !FileManager.default.fileExists(atPath: url.path) { _ = FileManager.default.createFile(atPath: url.path, contents: nil) }
            let file = try FileHandle(forWritingTo: url); defer { try? file.close() }
            try file.seekToEnd()
            try file.write(contentsOf: Data((ISO8601DateFormatter().string(from: Date()) + " " + text + "\n").utf8))
        } catch { /* Diagnostics must not prevent stopping. */ }
    }
}
