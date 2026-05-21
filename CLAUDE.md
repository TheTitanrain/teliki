# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Windows digital signage player for fullscreen media playback. Targets Windows 7 SP1, .NET Framework 4.7.2, C# 7.3. Do not introduce newer language features or runtime dependencies.

## Build and Test

```powershell
dotnet restore Teliki.sln
dotnet build Teliki.sln -c Release
dotnet test Teliki.sln -v minimal
```

Run a single test class:
```powershell
dotnet test Teliki.Tests\Teliki.Tests.csproj --filter "ClassName~MediaScannerTests"
```

Run a single test method:
```powershell
dotnet test Teliki.Tests\Teliki.Tests.csproj --filter "FullyQualifiedName~ScanResult_IsPromoted"
```

Test framework: MSTest v3.

## Architecture

### Project layout

- **`Teliki.Core`** — pure logic, no WinForms dependency. Scanning, caching, config, playlist, display abstractions, data models.
- **`Teliki.App`** — WinForms host. Entry point, UI forms, background threading, OS integration.
- **`Teliki.Tests`** — MSTest suite; references both Core and App.

### Startup flow

`Program.cs` loads `appsettings.ini` via `ConfigLoader` → `AppConfigNormalizer` resolves relative paths → `SignageApplicationContext` wires everything together and starts the runtime.

### Key classes

| Class | Where | Role |
|---|---|---|
| `SignageController` | App | Pure-logic controller; manages timer lifecycle, config reloads, scan state machine, hotkey gating. Fully tested without WinForms. |
| `SignageApplicationContext` | App | `ApplicationContext` that owns timers, forms, background runner; implements `ISignageRuntime`; marshals scan results back to the UI thread via `_uiDispatcher.BeginInvoke`. |
| `BackgroundScanRunner` | App | Runs scan+cache on a background thread; raises `ScanCompleted` for `SignageApplicationContext` to marshal. |
| `DisplayForm` | App | Fullscreen borderless topmost WinForms window; implements `IMediaRenderer`. |
| `DisplayCoordinator` | Core | Fans `Advance`/`Render` out to all active `IMediaRenderer` instances; calls `PlaylistService.ReportFailure` on renderer errors. |
| `MediaScanner` | Core | Enumerates source folder for supported extensions, ordered case-insensitively by name. |
| `MediaCache` | Core | Copies stable files to cache using a temp-then-rename atomic pattern; promotes only after full successful copy; keeps last-known-good playlist on failure; emits blank only after 2 consecutive empty source scans. |
| `PlaylistService` | Core | Circular playlist; tracks per-item failure counts to skip broken files. |
| `ApplicationShutdownCoordinator` | App | Ensures a single coordinated exit when multiple `DisplayForm` windows close simultaneously. |
| `CursorVisibilityManager` | App | Hides cursor during playback, restores it when settings dialog is open. |

### Configuration

`appsettings.ini` sits next to `Teliki.App.exe`. `ConfigLoader` reads raw key/value pairs into `AppConfig`. `AppConfigNormalizer` resolves relative `MediaFolder` paths against the application base directory and expands `%LocalAppData%` in `CacheFolder`. `ConfigFileStore` + `ConfigDocument` handle round-trip editing without losing unknown keys.

Settings saved from the UI write atomically to `appsettings.ini`, then reload via `ConfigLoader` and call `SignageController.ApplyConfig` which restarts timers and triggers a fresh scan. Display-mode changes are applied after the settings dialog closes by rebuilding `DisplayForm` instances.

### Testability seams

- `IFileSystem` / `PhysicalFileSystem` — all file I/O abstracted for unit tests.
- `ISignageRuntime` — lets `SignageControllerTests` drive the controller without any WinForms.
- `IAppTimer` / `WinFormsTimerAdapter` — timer control abstracted so tests call tick handlers directly.
- `IScreenProvider` / `WindowsScreenProvider` — screen enumeration abstracted for display-selector tests.
