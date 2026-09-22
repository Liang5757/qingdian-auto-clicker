# Qingdian Auto Clicker · 轻点

A small, open-source Windows and macOS auto clicker with a Chinese desktop UI. **F9 starts; F10 stops**, globally. No network access, telemetry, or third-party runtime packages.

[中文](README.md) · [Downloads](https://github.com/Liang5757/qingdian-auto-clicker/releases) · [Contributing](CONTRIBUTING.md)

![Screenshot](docs/images/screenshot.png)

## Features

- Configurable interval from 20 ms to one hour.
- Left, right, or middle button; single or double clicks.
- A fixed number of rounds, or unlimited rounds. A double-click round contains two clicks.
- Click at the current pointer location without moving the pointer.
- One-second delay before clicking starts.
- Modern light UI with segmented choices and automatic settings persistence.
- Closing/minimizing hides to the system tray without stopping. Tray actions restore, start, stop, and exit; exit stops output.
- Optional hidden startup and per-user Windows login startup, both off by default. Launch never starts clicking automatically.
- Single instance; clicking disabled if the stop hotkey is unavailable.

## macOS

Native SwiftUI / AppKit app for macOS 13+, with one universal ZIP for Apple Silicon and Intel. Move `轻点.app` into Applications and grant Accessibility permission from the app's permission guide. F9 starts, F10 / Esc stops (Fn may be required on media-key keyboards). Missing permission or unavailable F10 blocks starting. Closing/minimizing keeps the app in the menu bar; use its menu to quit. Optional login startup uses SMAppService and never automatically starts clicking.

The Mac download is ad-hoc signed, **not Developer ID signed or notarized**. Gatekeeper may block first launch; use the system's approved opening flow after verifying the source, or build from source. Do not disable system protections. See [macOS documentation](macos/README.md).

Windows C# and macOS Swift implementations share a product contract, repository and release version, not a common runtime implementation. Linux and background injection into minimized target windows are not supported. Build macOS with `cd macos && swift test -c release`; package from the repository root with `bash scripts/package-macos.sh`.

## Windows run

Download and extract a release ZIP, then run `Qingdian.AutoClicker.exe`. Requires Windows 10/11 and .NET Framework 4.8 or newer 4.x. The development SDK is not required. Releases are currently unsigned.

Settings live in `%LOCALAPPDATA%\WindowsAutoClicker\settings.xml`. Exit the app and delete that file to reset settings. Disable login startup before uninstalling, exit via the tray, then delete the extracted folder; settings can be deleted separately. Login startup stores the quoted executable path in the current user Run registry key, only when enabled. Re-enable after moving the EXE. Tray residency still clicks the current pointer location; it does not send background clicks to minimized target windows.

Since v1.0.3, a dedicated message thread captures F10 / Esc key-down events and polls key state every approximately 10 ms. Stop requests remain latched. All sends pass through a serialized stop gate. Desktop access restrictions and input interception still apply.

Local diagnostics record settings, process/version, send counts and tagged input observations (approximately 1 MB maximum, no uploads). Open them from the UI. Untagged events do not identify the originating process or rule out forwarding by another program. The reported continuing right clicks after exit still require live diagnosis; this release does not establish their root cause.

## Build and test

On Windows, install .NET SDK 8.0.200 or a newer 8.0 feature band:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/package.ps1
```

The solution uses SDK-style projects targeting .NET Framework 4.8. Locked NuGet restore, compilation, and xUnit tests must pass before packaging. ZIP and SHA-256 files are written to `artifacts/`.

## Limits

Windows scheduling affects timing. Elevated targets may require matching privileges; some applications ignore synthetic input. Secure/locked desktops are unsupported. Use only where automation is permitted.

Automated tests cover logic, persistence, native layout, and message handling, not end-to-end input delivery on every desktop. Full manual Windows/DPI/privilege acceptance is still pending; see [testing](docs/TESTING.md).

Licensed under [MIT](LICENSE). See [architecture](docs/ARCHITECTURE.md), [contributing](CONTRIBUTING.md), and [security](SECURITY.md).
