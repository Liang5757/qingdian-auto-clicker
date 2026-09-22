import AppKit
let directory = URL(fileURLWithPath: CommandLine.arguments[1]).appendingPathComponent("AppIcon.iconset")
try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
for size in [16, 32, 128, 256, 512] {
    for scale in [1, 2] {
        let pixels = size * scale
        let bitmap = NSBitmapImageRep(bitmapDataPlanes: nil, pixelsWide: pixels, pixelsHigh: pixels, bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true, isPlanar: false, colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0)!
        NSGraphicsContext.saveGraphicsState()
        NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: bitmap)
        let transform = NSAffineTransform()
        transform.scale(by: CGFloat(pixels) / 512)
        transform.concat()
        NSColor(calibratedRed: 0.09, green: 0.44, blue: 0.96, alpha: 1).setFill()
        NSBezierPath(roundedRect: NSRect(x: 26, y: 26, width: 460, height: 460), xRadius: 100, yRadius: 100).fill()
        let path = NSBezierPath(); path.move(to: NSPoint(x: 163, y: 390))
        for p in [NSPoint(x:163,y:136), NSPoint(x:219,y:198), NSPoint(x:272,y:104), NSPoint(x:318,y:132), NSPoint(x:264,y:223), NSPoint(x:350,y:223)] { path.line(to:p) }
        path.close(); NSColor.white.setFill(); path.fill()
        NSGraphicsContext.restoreGraphicsState()
        let suffix = scale == 2 ? "@2x" : ""
        try bitmap.representation(using: .png, properties: [:])!.write(to: directory.appendingPathComponent("icon_\(size)x\(size)\(suffix).png"))
    }
}
