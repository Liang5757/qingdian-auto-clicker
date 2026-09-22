// swift-tools-version: 5.9
import PackageDescription
let package = Package(
    name: "Qingdian", platforms: [.macOS(.v13)],
    products: [.executable(name: "Qingdian", targets: ["Qingdian"])],
    targets: [
        .target(name: "QingdianCore"),
        .executableTarget(name: "Qingdian", dependencies: ["QingdianCore"]),
        .testTarget(name: "QingdianCoreTests", dependencies: ["QingdianCore"])
    ])
