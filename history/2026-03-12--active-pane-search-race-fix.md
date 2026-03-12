## Summary

Fixed a post-spec regression where the active-pane search smoke test could fail with `Collection was modified; enumeration operation may not execute.`

## What changed

- Added `CancelPendingDirectoryLoad()` in `FileListViewModel` to invalidate and cancel any in-flight directory load.
- Called that helper before both async and sync search execution so search results cannot race against a stale `LoadDirectoryAsync` completion.

## Why

The right-pane search flow can begin immediately after a navigation that still has an async folder load in flight. Without canceling that load, the stale completion can repopulate `Items` while search-result assertions are enumerating it.

## Verification

- `dotnet build CubicAIExplorer.sln -v minimal`
- `dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal`
- `tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe`

## Follow-up notes

- The smoke-project build may emit transient file-lock retry warnings on Windows if the prior smoke executable has not fully exited yet; rerunning is sufficient when the build eventually succeeds.
