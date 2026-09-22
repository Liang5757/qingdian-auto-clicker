import XCTest
@testable import QingdianCore

final class ClickEngineTests: XCTestCase {
    func testStopRejectsQueuedTicks() {
        var calls = 0
        let engine = ClickEngine { _ in calls += 1; return true }
        let token = engine.start(ClickSettings())
        XCTAssertTrue(engine.tick(generation: token)); engine.stop()
        for _ in 0..<1000 { XCTAssertFalse(engine.tick(generation: token)) }
        XCTAssertEqual(calls, 1)
    }
    func testOldGenerationCannotClickAfterRestart() {
        var calls = 0
        let engine = ClickEngine { _ in calls += 1; return true }
        let old = engine.start(ClickSettings()); engine.stop()
        let current = engine.start(ClickSettings())
        XCTAssertFalse(engine.tick(generation: old)); XCTAssertTrue(engine.tick(generation: current))
        XCTAssertEqual(calls, 1)
    }
    func testLimitAndDuplicateStart() {
        let engine = ClickEngine { _ in true }
        var s = ClickSettings(); s.limit = 2; s.doubleClick = true
        let token = engine.start(s)
        XCTAssertEqual(engine.start(ClickSettings()), token)
        XCTAssertTrue(engine.tick(generation: token)); XCTAssertTrue(engine.running)
        XCTAssertTrue(engine.tick(generation: token)); XCTAssertFalse(engine.running)
        XCTAssertEqual(engine.rounds, 2)
        XCTAssertFalse(engine.tick(generation: token))
    }
    func testFailureStopsWithoutCounting() {
        let engine = ClickEngine { _ in false }
        XCTAssertFalse(engine.tick(generation: engine.start(ClickSettings())))
        XCTAssertFalse(engine.running); XCTAssertTrue(engine.failed); XCTAssertEqual(engine.rounds, 0)
    }
    func testNormalization() {
        var s = ClickSettings(); s.interval = Int.min; s.limit = Int.max; s.button = -2
        XCTAssertEqual(s.normalized.interval, 20); XCTAssertEqual(s.normalized.limit, 100_000_000)
        XCTAssertEqual(s.normalized.button, 0)
        s.interval = Int.max; s.limit = -1; s.button = 7
        XCTAssertEqual(s.normalized.interval, 3_600_000); XCTAssertEqual(s.normalized.limit, 0)
        XCTAssertEqual(s.normalized.button, 2)
    }
    func testSettingsCompatibilityAndRoundTrip() throws {
        let dir = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        defer { try? FileManager.default.removeItem(at: dir) }
        let file = SettingsFile(url: dir.appendingPathComponent("settings.json"))
        XCTAssertEqual(try file.load(), ClickSettings())
        try FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true)
        try Data("{\"interval\":500}".utf8).write(to: file.url)
        var s = try file.load(); XCTAssertEqual(s.interval, 500); XCTAssertFalse(s.startHidden)
        s.startHidden = true; s.button = 2; s.doubleClick = true
        try file.save(s); XCTAssertEqual(try file.load(), s)
        try Data("broken".utf8).write(to: file.url)
        XCTAssertThrowsError(try file.load())
        XCTAssertEqual(try String(contentsOf: file.url), "broken")
    }
    func testOversizedSettingsRejected() throws {
        let path = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        defer { try? FileManager.default.removeItem(at: path) }
        try Data(repeating: 32, count: 65_537).write(to: path)
        XCTAssertThrowsError(try SettingsFile(url: path).load())
    }
    func testAllButtonsAndDoubleClickReachSender() {
        for button in 0...2 {
            for double in [false, true] {
                var s = ClickSettings(); s.button = button; s.doubleClick = double
                let engine = ClickEngine { received in XCTAssertEqual(received, s); return true }
                XCTAssertTrue(engine.tick(generation: engine.start(s)))
            }
        }
    }
}
