Completed spec 020 code review fixes.

- Centralized path normalization in `PathSecurityHelper` and applied it to bookmark/settings/template env overrides so override paths are always normalized before use.
- Switched single-instance pipe creation to `PipeOptions.CurrentUserOnly`; this preserves the intended same-user IPC restriction without pulling in extra pipe ACL assemblies.
- Added `IDialogService` with WPF and headless implementations so `FileListViewModel` and `MainViewModel` no longer call `MessageBox.Show` or touch `Application.Current.MainWindow` directly.
- Reworked archive browsing to pass an extraction delegate through `ArchiveBrowseRequest`, removing the model-to-viewmodel dependency while keeping the dialog behavior unchanged.
- Made folder-tree expansion, directory loading, and directory-property counting asynchronous for UI callers, but kept headless/test paths deterministic to avoid smoke-test flakiness.
- Added `Dispose()` cleanup for the main, tab, and file-list viewmodels to detach event handlers and stop retaining queue/settings/bookmark subscriptions after tabs/windows close.
- Tightened replace-flow safety by removing check-then-act deletion in `PerformStageAndRename`, and removed clipboard stream ownership ambiguity by using a byte-array drop-effect payload.
- Verified with full solution build, smoke-test project build, and a full smoke run; added smoke coverage for sanitized env overrides and JSON Id round-trips.
