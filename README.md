# Teliki

Teliki is a Windows digital signage player for fullscreen media playback on advertising displays. It targets Windows 7 SP1 and .NET Framework 4.7.2.

## Behavior

- Opens one borderless fullscreen topmost window for each connected monitor.
- Uses the full screen bounds, including taskbar area.
- Hides the cursor while media is displayed.
- Opens a modal settings window with `F1`.
- Terminates the playback app with `Esc` when a playback window has focus.
- Cycles media by `IntervalSeconds`.
- Polls the configured source folder by `ScanIntervalSeconds`.
- Plays only from a local cache. Source files are never rendered directly.
- Keeps the last promoted cached playlist when the source folder, network share, or copy operation fails.
- Shows a black fullscreen window only after two consecutive successful empty source scans.

## Supported Media

Images:

- `.jpg`
- `.jpeg`
- `.png`
- `.bmp`
- `.gif`

Video:

- `.wmv`
- `.avi`
- `.mp4`

Video playback uses the Windows Media Player ActiveX control and installed system codecs. Codec support is best-effort, especially on Windows 7. Missing WMP, missing codecs, or broken media files are not fatal; failing files are skipped or quarantined after repeated renderer failures.

## Configuration

Copy `appsettings.sample.ini` to `appsettings.ini` next to `Teliki.App.exe`, or edit the `Teliki.App/appsettings.ini` file before publishing.

```ini
MediaFolder=media
IntervalSeconds=10
ScanIntervalSeconds=5
ScanTimeoutSeconds=30
CacheFolder=%LocalAppData%\Teliki\MediaCache
MaxCacheSizeMb=1024
MinFreeDiskMb=512
ScreenMode=AllScreens
```

`MediaFolder` can be a local folder or a UNC/network path. Relative paths are resolved from the application directory. Folder scanning is non-recursive in this version.

## Settings UI

- `F1` from a playback window opens the settings dialog on that screen.
- Opening the settings dialog makes the mouse cursor visible until the dialog closes, including on multi-monitor playback setups.
- `Esc` from a playback window closes the playback windows and terminates the process through a single coordinated shutdown request, even while several windows are closing at once.
- `Esc` inside the settings dialog closes only the dialog.
- The settings dialog edits `MediaFolder`, `IntervalSeconds`, `ScanIntervalSeconds`, and `ScanTimeoutSeconds`.
- Saving applies the new values immediately without restarting the app.

The dialog writes to the deployed `appsettings.ini` next to `Teliki.App.exe`. The account running the app must have write permission to that file and directory. If the write fails, the dialog stays open and the running configuration is unchanged.

## Cache And Logs

Default cache:

```text
%LocalAppData%\Teliki\MediaCache
```

Default log:

```text
%LocalAppData%\Teliki\logs\teliki.log
```

The cache uses a manifest for the active last-known-good playlist. New playlists are promoted only after a complete successful scan and full copy into versioned cache files. Active cached media is never deleted to satisfy cache limits; the app logs a warning instead.

## Build And Test

This repository uses SDK-style projects targeting `.NET Framework 4.7.2`.

On the current development machine, use the .NET SDK:

```powershell
dotnet restore Teliki.sln
dotnet build Teliki.sln -c Release
dotnet test Teliki.Tests\Teliki.Tests.csproj -c Release
```

On a Visual Studio Build Tools machine with classic .NET Framework tools available, the equivalent flow is:

```powershell
nuget restore Teliki.sln
msbuild Teliki.sln /p:Configuration=Release
vstest.console Teliki.Tests\bin\Release\net472\Teliki.Tests.dll
```

## Deployment

1. Install .NET Framework 4.7.2 on the target Windows 7 SP1 machine.
2. Ensure Windows Media Player is available if video playback is required.
3. Build Release.
4. Copy `Teliki.App\bin\Release\net472\` to the target machine.
5. Place or edit `appsettings.ini` next to `Teliki.App.exe`.
6. Put media files in the configured source folder.
7. Start `Teliki.App.exe`.

The app does not install autostart entries in this version. Configure Windows startup separately if needed.

## Version 1 Limits

- No installer or autostart setup.
- No hard keyboard blocking.
- No per-screen media folders.
- No recursive media folders.
- No remote management.
- No codec-independent video decoding.
