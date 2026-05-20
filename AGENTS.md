# AGENTS.md

## Project Scope
- `Teliki` is a Windows digital signage player for fullscreen media playback on advertising displays.
- Target platform: Windows 7 SP1.
- Target framework: .NET Framework 4.7.2.

## Repository Layout
- `Teliki.App` contains the Windows application and settings UI.
- `Teliki.Core` contains core playback, caching, scanning, and configuration logic.
- `Teliki.Tests` contains automated tests for the solution.
- `Teliki.sln` is the root solution file and should be used for full-project build and test runs.

## Working Rules
- Keep changes aligned with the current product behavior described in `README.md`.
- Prefer focused fixes and avoid unrelated refactors.
- Preserve compatibility with the existing Windows and .NET Framework targets.
- Do not introduce new runtime dependencies unless they are necessary for the task.

## Build And Test
- Restore and build through the solution when validation requires it.
- Run the full test suite with `dotnet test Teliki.sln -v minimal`.
- If tests fail, fix the failures before considering the task complete.

## Documentation
- Update `README.md` when user-visible behavior, configuration, supported media, cache behavior, or operational workflow changes.
- Keep configuration examples and documented shortcuts in sync with the implementation.

## Completion Checklist
- Verify the requested change in the affected project(s).
- Run relevant tests, and for cross-cutting changes run `dotnet test Teliki.sln -v minimal`.
- Review documentation impact and update it when needed.
- Commit the completed work with a descriptive message.
