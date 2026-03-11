# 020 - Code Review Fixes

## Status

INCOMPLETE

## Summary

Address issues found during a full codebase code review covering security, architecture/MVVM, bugs, memory leaks, performance, serialization, and input validation.

## Tasks

### Security (Critical)

- [ ] **S1: Sanitize environment variable paths** — `CUBICAI_BOOKMARKS_PATH`, `CUBICAI_SETTINGS_PATH`, and `CUBICAI_NEWFILE_TEMPLATES_PATH` are read directly without `SanitizePath()`, allowing path traversal. Files: `BookmarkService.cs:32-34`, `SettingsService.cs:32-34`, `UserSettings.cs:9-11`
- [ ] **S2: Add ACL to named pipe** — `SingleInstanceService.cs:61-62` creates the pipe with default access, allowing any user on the machine to send commands. Restrict to current user via `PipeSecurity`/`PipeAccessRule`.

### Serialization Bugs (High)

- [ ] **D1: TabItem.Id is get-only** — `System.Text.Json` can't deserialize it; generates new Guid each load. Change to `{ get; set; }` or `{ get; init; }`. File: `TabItem.cs:5`
- [ ] **D2: BookmarkItem.Id is get-only** — same issue. File: `BookmarkItem.cs:8`
- [ ] **D3: SavedSearchItem.Id is get-only** — same issue. File: `SavedSearchItem.cs:7`

### Architecture / MVVM Violations (High)

- [ ] **A1: Extract MessageBox.Show from ViewModels** — 22 occurrences across `FileListViewModel.cs` (18x) and `MainViewModel.cs` (4x). Create a dialog service interface and inject it so ViewModels don't depend on WPF UI.
- [ ] **A2: Remove Application.Current.MainWindow from ViewModel** — `FileListViewModel.cs:274,1458,1463` accesses UI directly. Route through a service or event.
- [ ] **A3: Remove ViewModel reference from ArchiveBrowseRequest model** — `ArchiveBrowseRequest.cs:9` holds a `FileListViewModel` reference. Models should not depend on ViewModels.

### Performance (High)

- [ ] **P1: Make folder tree expansion async** — `FolderTreeNodeViewModel.cs:31-56` synchronously enumerates subdirectories on the UI thread, freezing for large dirs.
- [ ] **P2: Make LoadDirectory async** — `FileListViewModel.cs:379` blocks UI for large directories.
- [ ] **P3: Make PropertiesDialog file counting async** — `PropertiesDialog.xaml.cs:33-35` synchronously counts files/dirs in constructor.

### Bugs & Race Conditions (High/Medium)

- [ ] **B1: TOCTOU race in PerformStageAndRename** — `FileSystemService.cs:1132-1140` does check-then-act on directory/file existence. Use try/catch instead of pre-checking.
- [ ] **B2: MemoryStream leak in clipboard** — `ClipboardService.cs:31,55` creates a stream with unclear ownership. Ensure proper disposal.
- [ ] **B3: Replace Thread.Sleep with async delay** — `BookmarkService.cs:45-65` and `SettingsService.cs:45-62` use `Thread.Sleep(100)` in retry loops, blocking UI thread.

### Memory Leaks (Medium)

- [ ] **M1: Unsubscribe _fileOperationQueueService.PropertyChanged** — `MainViewModel.cs:244` subscribes but never unsubscribes.
- [ ] **M2: Unsubscribe SettingsChanged and BookmarksChanged** — `MainViewModel.cs:247,250` subscribes but never unsubscribes.
- [ ] **M3: Fix anonymous lambda on PropertyChanged** — `FileListViewModel.cs:356-365` uses anonymous lambda that can't be unsubscribed. Store reference.
- [ ] **M4: Unsubscribe NavigateRequested and Navigated on tab close** — `TabViewModel.cs:54-55` subscribes but never unsubscribes.

### Input Validation (Low)

- [ ] **V1: Validate output directory exists in SplitFileDialog** — `SplitFileDialog.xaml.cs:79-100` only checks non-empty string.
- [ ] **V2: Validate destination path in ExtractArchiveDialog** — `ExtractArchiveDialog.xaml.cs:22-28` accepts any non-empty string.
- [ ] **V3: Null-coalesce ExtensionTextBox.Text** — `BatchRenameDialog.xaml.cs:101` is missing `?? string.Empty`.

## Notes

- Prioritize S1, D1-D3, and A1 first as they have the highest impact.
- The MVVM MessageBox extraction (A1) is the largest task — consider a simple `IDialogService` with `ShowMessage`/`ShowConfirmation` methods.
- Memory leak fixes (M1-M4) can be addressed together by adding `IDisposable` to ViewModels and unsubscribing in `Dispose()`.
- TOCTOU races (B1) are inherent to file system operations but can be mitigated by switching from check-then-act to try-catch patterns.
