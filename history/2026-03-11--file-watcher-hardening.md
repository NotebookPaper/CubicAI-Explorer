# File Watcher Hardening

- Added a shared debounced watcher helper for JSON-backed app state files so settings/bookmark sync handles create, change, delete, rename, and replace flows consistently.
- Recreate the underlying `FileSystemWatcher` after watcher errors and trigger a best-effort reload so the app resynchronizes after overflow or directory-level watcher faults.
- Wrapped service-owned saves in watcher suppression to avoid self-triggered reload loops while preserving external change notifications.
- Added smoke coverage for external settings delete/recreate and bookmark temp-file replacement flows.
- Validation completed with `dotnet build CubicAIExplorer.sln -v minimal`, `dotnet build tests/CubicAIExplorer.SmokeTests\CubicAIExplorer.SmokeTests.csproj -v minimal`, and `tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe`.
