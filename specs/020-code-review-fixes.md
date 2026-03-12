# 020 - Code Review Fixes
<!-- NR_OF_TRIES: 1 -->

## Status: COMPLETE

## Summary

Address issues found during a full codebase code review covering security, architecture/MVVM, bugs, memory leaks, performance, serialization, and input validation.

## Tasks

### Security (Critical)

- [x] **S1: Sanitize environment variable paths** — `CUBICAI_BOOKMARKS_PATH`, `CUBICAI_SETTINGS_PATH`, and `CUBICAI_NEWFILE_TEMPLATES_PATH` now resolve through the shared path sanitizer before use.
- [x] **S2: Add ACL to named pipe** — single-instance IPC now uses `PipeOptions.CurrentUserOnly` so only the current user can connect to the server pipe.

### Serialization Bugs (High)

- [x] **D1: TabItem.Id is get-only** — `System.Text.Json` can now round-trip the persisted identifier.
- [x] **D2: BookmarkItem.Id is get-only** — `System.Text.Json` can now round-trip the persisted identifier.
- [x] **D3: SavedSearchItem.Id is get-only** — `System.Text.Json` can now round-trip the persisted identifier.

### Architecture / MVVM Violations (High)

- [x] **A1: Extract MessageBox.Show from ViewModels** — added `IDialogService` plus WPF/headless implementations and routed viewmodel message/confirmation flows through it.
- [x] **A2: Remove Application.Current.MainWindow from ViewModel** — batch-rename and conflict dialogs now flow through the dialog service instead of directly touching `MainWindow`.
- [x] **A3: Remove ViewModel reference from ArchiveBrowseRequest model** — archive browse requests now carry an extraction delegate instead of a `FileListViewModel`.

### Performance (High)

- [x] **P1: Make folder tree expansion async** — folder-tree child enumeration now runs asynchronously with a loading placeholder.
- [x] **P2: Make LoadDirectory async** — directory enumeration now uses `LoadDirectoryAsync` for UI flows while preserving deterministic headless behavior for smoke tests.
- [x] **P3: Make PropertiesDialog file counting async** — directory counts now populate after load on a background task.

### Bugs & Race Conditions (High/Medium)

- [x] **B1: TOCTOU race in PerformStageAndRename** — replace staging now uses try/catch-based backup/delete helpers instead of check-then-act deletion.
- [x] **B2: MemoryStream leak in clipboard** — preferred drop-effect data now uses a byte array payload instead of a disposable stream.
- [x] **B3: Replace Thread.Sleep with async delay** — retry loops now use async delays and async save paths.

### Memory Leaks (Medium)

- [x] **M1: Unsubscribe _fileOperationQueueService.PropertyChanged** — `MainViewModel` now detaches queue handlers in `Dispose()`.
- [x] **M2: Unsubscribe SettingsChanged and BookmarksChanged** — `MainViewModel` now detaches external service handlers in `Dispose()`.
- [x] **M3: Fix anonymous lambda on PropertyChanged** — `FileListViewModel` now stores the queue handler delegate and unsubscribes it in `Dispose()`.
- [x] **M4: Unsubscribe NavigateRequested and Navigated on tab close** — `TabViewModel` now detaches these handlers and disposes its file list.

### Input Validation (Low)

- [x] **V1: Validate output directory exists in SplitFileDialog** — split now requires an existing output directory.
- [x] **V2: Validate destination path in ExtractArchiveDialog** — extract now validates the resolved path and its parent directory before accepting.
- [x] **V3: Null-coalesce ExtensionTextBox.Text** — batch rename now safely treats a null extension textbox value as an empty string.

## Notes

- Prioritize S1, D1-D3, and A1 first as they have the highest impact.
- The MVVM MessageBox extraction (A1) is the largest task — consider a simple `IDialogService` with `ShowMessage`/`ShowConfirmation` methods.
- Memory leak fixes (M1-M4) can be addressed together by adding `IDisposable` to ViewModels and unsubscribing in `Dispose()`.
- TOCTOU races (B1) are inherent to file system operations but can be mitigated by switching from check-then-act to try-catch patterns.
