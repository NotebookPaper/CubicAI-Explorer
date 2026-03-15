# 023 - Code Review Fixes v2
<!-- NR_OF_TRIES: 1 -->

## Status: COMPLETE

## Summary

Address issues found during a second full codebase code review (2026-03-15) covering bugs, security, resource management, and threading.

## Tasks

### Bugs (Critical)

- [x] **B1: Search "Include Hidden/System" flags invert filter logic** — `FileListViewModel.cs` now treats hidden/system flags as inclusion switches instead of exclusion-only filters, and the advanced-search UI text now matches the corrected behavior.

- [x] **B2: Saved search results overwritten by async directory load race** — saved-search replay now awaits pane navigation/load completion before applying criteria, and smoke coverage verifies the search result set survives the navigation load.

- [x] **B3: `SyncFolderTreeToPath` resets guard before async expand completes** — tree sync now uses a versioned async task flow so stale expansions are discarded instead of racing selection state.

- [x] **B4: `ExpandToPathAsync` path prefix match without separator guard** — tree path matching now uses same-or-child comparison with a separator guard.

### Security (High)

- [x] **S1: IPC named pipe has no message size limit** — pipe reads now use a bounded raw-byte reader with a 4096-byte ceiling, and smoke coverage verifies oversized payload rejection.

- [x] **S2: Zip extraction path traversal — prefix check without separator** — archive extraction now uses a separator-aware same-or-child directory check, and smoke coverage verifies sibling-prefix traversal is rejected.

- [x] **S3: `ShowBookmarkProperties` bypasses path sanitization** — bookmark properties now sanitize the stored path and ignore transient filesystem races safely.

- [x] **S4: `LaunchExternalTool` argument injection via trailing backslash** — external-tool path quoting now follows Windows command-line escaping rules so trailing backslashes remain inside the quoted argument.

- [x] **S5: `ShellContextMenuHelper` uses unsynchronized static state** — shell context menu entry points now serialize access around the static COM/window-proc state and always restore the original window procedure in cleanup.

### Resource Management (Medium)

- [x] **R1: `CancellationTokenSource` leaked on every folder navigation** — directory-load CTS instances are now canceled and disposed during replacement/cancel flows.

- [x] **R2: `FileOperationQueueService` CTS disposed before UI-thread cleanup** — queue CTS disposal now happens after busy-state cleanup, and smoke coverage verifies later cancel requests remain safe.

- [x] **R3: `ExpandToPathAsync` is `async void` with no exception handler** — folder-tree expansion now runs as awaited `Task` work behind a guarded caller.

### Threading (Medium)

- [x] **T1: `.GetAwaiter().GetResult()` deadlock risk on UI thread** — the flagged sync wrappers now have true synchronous implementations instead of async-over-sync waits.

### Cleanup (Low)

- [x] **C1: Unfrozen static `SolidColorBrush` in MainWindow** — the static pane brushes now use the shared frozen-brush helper.

- [x] **C2: Delete dead `MainViewModel.GetBookmarksPath()` method** — removed the unused duplicate bookmark-path helper.

- [x] **C3: Fire-and-forget `_ = LoadDirectoryAsync(...)` silently swallows exceptions** — tab navigation now observes asynchronous load failures and surfaces them through the file-list status instead of silently dropping them.

## Notes

- Prioritize B1 and S2 first — B1 is a user-visible functional bug, S2 is a security gap.
- B2 (saved search race) requires careful async coordination — consider making the navigation await-able.
- T1 was resolved by replacing the flagged sync wrappers with true synchronous implementations while keeping the async paths for UI/background use.
- S5 (shell context menu statics) is unlikely to hit in practice since context menus are modal and single-threaded, but worth hardening.
