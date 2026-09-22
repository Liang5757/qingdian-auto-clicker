import Foundation

public struct ClickSettings: Codable, Equatable {
    public var interval = 100
    public var button = 0
    public var doubleClick = false
    public var limit = 0
    public var startHidden = false
    public init() {}
    enum CodingKeys: String, CodingKey { case interval, button, doubleClick, limit, startHidden }
    public init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        interval = try c.decodeIfPresent(Int.self, forKey: .interval) ?? 100
        button = try c.decodeIfPresent(Int.self, forKey: .button) ?? 0
        doubleClick = try c.decodeIfPresent(Bool.self, forKey: .doubleClick) ?? false
        limit = try c.decodeIfPresent(Int.self, forKey: .limit) ?? 0
        startHidden = try c.decodeIfPresent(Bool.self, forKey: .startHidden) ?? false
        self = normalized
    }
    public var normalized: Self {
        var s = self
        s.interval = min(3_600_000, max(20, interval))
        s.button = min(2, max(0, button))
        s.limit = min(100_000_000, max(0, limit))
        return s
    }
}

// Used only on the caller's serial executor (the app uses the main thread).
// Each scheduled callback carries its generation so a previous run cannot click after restart.
public final class ClickEngine {
    public private(set) var running = false
    public private(set) var rounds = 0
    public private(set) var generation = 0
    public private(set) var failed = false
    private var settings = ClickSettings()
    private let send: (ClickSettings) -> Bool
    public init(send: @escaping (ClickSettings) -> Bool) { self.send = send }
    @discardableResult public func start(_ settings: ClickSettings) -> Int {
        guard !running else { return generation }
        generation += 1; self.settings = settings.normalized
        rounds = 0; failed = false; running = true
        return generation
    }
    public func stop() { running = false; generation += 1 }
    @discardableResult public func tick(generation token: Int) -> Bool {
        guard running, token == generation else { return false }
        guard send(settings) else { failed = true; stop(); return false }
        rounds += 1
        if settings.limit > 0 && rounds >= settings.limit { stop() }
        return true
    }
}

public final class SettingsFile {
    public let url: URL
    public init(url: URL) { self.url = url }
    public func load() throws -> ClickSettings {
        guard FileManager.default.fileExists(atPath: url.path) else { return ClickSettings() }
        let handle = try FileHandle(forReadingFrom: url)
        defer { try? handle.close() }
        let data = try handle.read(upToCount: 65_537) ?? Data()
        guard data.count <= 65_536 else { throw CocoaError(.fileReadTooLarge) }
        return try JSONDecoder().decode(ClickSettings.self, from: data)
    }
    public func save(_ settings: ClickSettings) throws {
        try FileManager.default.createDirectory(at: url.deletingLastPathComponent(), withIntermediateDirectories: true)
        try JSONEncoder().encode(settings.normalized).write(to: url, options: .atomic)
    }
}
