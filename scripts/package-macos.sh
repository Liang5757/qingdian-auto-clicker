#!/bin/bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VERSION="$(awk -F '[<>]' '/<Version>/{print $3; exit}' "$ROOT/Directory.Build.props")"
[[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo "Invalid project version" >&2; exit 1; }
cd "$ROOT/macos"
swift test -c release
for ARCH in arm64 x86_64; do
  swift build -c release --arch "$ARCH" --build-path ".build/$ARCH"
done
ARM="$(swift build -c release --arch arm64 --build-path .build/arm64 --show-bin-path)/Qingdian"
INTEL="$(swift build -c release --arch x86_64 --build-path .build/x86_64 --show-bin-path)/Qingdian"
mkdir -p "$ROOT/artifacts"
STAGE="$(mktemp -d "$ROOT/artifacts/macos-stage.XXXXXX")"
APP="$STAGE/轻点.app"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
lipo -create "$ARM" "$INTEL" -output "$APP/Contents/MacOS/Qingdian"
lipo -verify_arch arm64 x86_64 "$APP/Contents/MacOS/Qingdian"
cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleExecutable</key><string>Qingdian</string>
<key>CFBundleIdentifier</key><string>io.github.liang5757.qingdian</string>
<key>CFBundleName</key><string>轻点</string>
<key>CFBundleDisplayName</key><string>轻点</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>CFBundleShortVersionString</key><string>$VERSION</string>
<key>CFBundleVersion</key><string>$VERSION</string>
<key>LSMinimumSystemVersion</key><string>13.0</string>
<key>NSHighResolutionCapable</key><true/>
<key>NSPrincipalClass</key><string>NSApplication</string>
<key>CFBundleIconFile</key><string>AppIcon</string>
</dict></plist>
PLIST
swift Tools/MakeIcon.swift "$STAGE"
iconutil -c icns "$STAGE/AppIcon.iconset" -o "$APP/Contents/Resources/AppIcon.icns"
plutil -lint "$APP/Contents/Info.plist"
# Ad-hoc signing supplies structural integrity, not a Developer ID or notarization.
codesign --force --sign - "$APP"
codesign --verify --deep --strict "$APP"
cp "$ROOT/LICENSE" "$ROOT/macos/README.md" "$ROOT/CHANGELOG.md" "$STAGE/"
ARCHIVE="$ROOT/artifacts/qingdian-auto-clicker-$VERSION-macos-universal.zip"
# Package only the app and user-facing documents, excluding intermediate iconsets.
PACKAGE="$STAGE/package"
mkdir -p "$PACKAGE"
mv "$APP" "$PACKAGE/"
cp "$STAGE/LICENSE" "$STAGE/README.md" "$STAGE/CHANGELOG.md" "$PACKAGE/"
ditto -c -k --sequesterRsrc "$PACKAGE" "$ARCHIVE"
(cd "$ROOT/artifacts" && shasum -a 256 "$(basename "$ARCHIVE")" > "$(basename "$ARCHIVE").sha256")
echo "Package: $ARCHIVE"
