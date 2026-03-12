# Continuation Instructions for Next Session

> Last updated: 2026-03-12
> Branch: `master`
> HEAD: current local `master` after spec 020
> Status: Spec 020 is implemented and verified.

Continue in `C:\dev\CubicAI_rewrite` on `CubicAIExplorer.sln`.

## Status

- Local `master` contains the latest verified roadmap slices in this checkout.
- The branch builds and the smoke harness passes, including queue failure-history coverage and the forced symbolic-link failure regression.
- Specs `001`, `002`, `003`, `004`, `005`, `006`, `007`, `008`, `009-empty-recycle-bin`, `010-shell-verb-execution`, and `011-file-watcher-hardening` are complete in this checkout.
- Spec `009-batch-rename` is now complete in this checkout.
- Spec `010-breadcrumb-dropdowns` is now complete in this checkout.
- Spec `011-content-search` is now complete in this checkout.
- Spec `012-bookmarks-bar` is now complete in this checkout.
- Spec `013-tab-locking-coloring` is now complete in this checkout.
- Spec `014-file-utilities` is now complete in this checkout.
- Spec `015-layout-manager` is now complete in this checkout.
- Spec `016-advanced-search-filters` is now complete in this checkout.
- Spec `017-grouping-manual-sort` is now complete in this checkout.
- Spec `018-external-tools` is now complete in this checkout.
- Spec `019-drop-stack` is now complete in this checkout.
- Spec `020-code-review-fixes` is now complete in this checkout.
- The remaining post-spec roadmap item, improved queue-history error reporting, is also complete in this checkout.
- Remaining untracked paths are mostly local Ralph/tooling folders and user-local design scratch files.

## Latest Completed

- **Spec 020: Code Review Fixes** (New in this session)
  - Added shared path sanitization for bookmark/settings/template env overrides plus current-user-only single-instance pipe creation.
  - Introduced `IDialogService` so `FileListViewModel` and `MainViewModel` no longer call `MessageBox.Show` or `Application.Current.MainWindow` directly.
  - Reworked archive browse requests, async directory/folder/property loading, replace-flow cleanup, clipboard drop-effect payload handling, and viewmodel disposal/unsubscription.
  - Added smoke coverage for sanitized env overrides and JSON Id round-trips for tab, bookmark, and saved-search models.

## Completed

- **Spec 018: External Tools Configuration** (New in this session)
  - Added an `External Tools` tab in Preferences with editable name/path/arguments rows plus add, browse, and remove actions.
  - Persisted user-defined tools in `UserSettings` and surfaced them through `MainViewModel` so settings reloads keep the menu catalog current.
  - Added `Tools > External Tools` and pane `Open with... (Tools)` submenus, with launches routed through `IFileSystemService.LaunchExternalTool`.
  - Added smoke coverage for settings round-trip persistence, window loading, argument expansion, launch routing, and XAML/code-behind wiring.
- **Spec 019: Drop Stack (Virtual Collection)** (New in this session)
  - Added a toggleable `Drop Stack` sidebar pane with View-menu visibility control, drag/drop collection, bulk copy/move actions, and clear/remove affordances.
  - Kept the shelf non-destructive during collection so dragging items into the pane only records source paths and delete actions only remove the shelf entry.
  - Routed `Copy all to...` and `Move all to...` through the existing queue-backed filesystem transfer path and refreshed open panes after successful transfers.
  - Added smoke coverage for cross-folder collection persistence, entry removal, copy retention, move clearing, and XAML/code-behind wiring.
- **Roadmap: Queue history error reporting** (New in this session)
  - Added bounded recent-operation history to `FileOperationQueueService` with explicit success, failure, and canceled states plus retained detail text.
  - Exposed the recent queue history through `MainViewModel` and wired the existing status-bar queue-details toggle to a popup showing active progress and recent results.
  - Added smoke coverage for failed queue operations and XAML wiring checks for the queue-details popup/history bindings.
- **Reliability: Headless symbolic-link failure handling** (New in this session)
  - Updated `FileListViewModel.CreateSymbolicLinkWithHistory` so headless callers rethrow symbolic-link failures instead of trying to open a blocking modal dialog.
  - Added smoke coverage with a throwing filesystem test double to verify the original privilege error reaches the caller and no undo history is recorded on failure.
- **Spec 009: Advanced Batch Rename** (New in this session)
  - Added `BatchRenameDialog` with live preview columns for original and computed new names.
  - Added `BatchRenameService` to compute collision-safe preview names and apply grouped two-phase renames safely.
  - Updated `FileListViewModel.Rename` so multi-select rename routes through the batch dialog while single-item rename stays inline.
  - Added smoke coverage for preview generation, command routing, and grouped undo/redo.
- **Spec 010: Breadcrumb Dropdown Navigation** (New in this session)
  - Added per-segment dropdown buttons to the breadcrumb bar for every path segment except the current folder.
  - Populated breadcrumb branch menus asynchronously from `IFileSystemService.GetSubDirectories` with loading and empty-folder placeholder states.
  - Routed dropdown selections through the existing current-pane navigation path so tab history stays intact.
  - Added smoke coverage for dropdown loading, branch navigation, back-history preservation, and XAML/code-behind wiring.
- **Spec 011: Content Search** (New in this session)
  - Added an optional content-search field and `Include Content` toggle to the search bar so folder search can target file contents in addition to names.
  - Extended `FileListViewModel` recursive search to combine filename matching with safe chunked text scanning for eligible file types only.
  - Skipped files larger than 10 MB and non-text extensions so grep-style searches stay responsive and avoid binary scans.
  - Persisted content-search criteria through saved searches and added smoke coverage for content-only search, search limits, saved-search replay, and XAML wiring.
- **Spec 012: Horizontal Bookmarks Bar** (New in this session)
  - Added a toggleable bookmarks bar below the address area bound to the top-level bookmark collection.
  - Added bookmark-bar button navigation, direct rename/delete context actions, and a separate persisted `ShowBookmarksBar` setting.
  - Added drag/drop folder bookmarking onto the bar through the existing bookmark persistence path with duplicate suppression.
  - Added smoke coverage for bookmarks-bar visibility persistence, navigation routing, drop-path persistence, and XAML wiring.
- **Spec 013: Tab Locking and Coloring** (New in this session)
  - Added persisted tab/session state for locked tabs and tab colors while preserving legacy `OpenTabs` compatibility.
  - Locked tabs now allow descendant-folder navigation in place and fork unrelated navigation into a new tab, including back/forward history moves.
  - Added tab context-menu actions for lock toggling and palette-based tab coloring plus header lock/color indicators.
  - Added smoke coverage for locked-tab fork navigation, state persistence across reload, and XAML wiring.
- **Spec 014: File Utilities** (New in this session)
  - Added `Tools` menu commands and standalone dialogs for splitting files, joining numbered chunks, and generating checksums.
  - Extended `IFileSystemService` with queue-backed split/join/checksum operations so long-running file utility work stays inside the service boundary.
  - Added contiguous chunk-sequence validation, partial-output cleanup on failure, and one-pass MD5/SHA1/SHA256 hashing with compare support.
  - Added smoke coverage for bit-perfect split/join round-trips, checksum generation/comparison, and tool-command wiring.
- **Spec 015: Layout Manager** (New in this session)
  - Added a persisted `WindowLayout` model in the existing settings file and exposed saved layouts through `MainViewModel`.
  - Added `View > Layouts` menu wiring for save/apply plus a lightweight manage-layouts dialog for apply/delete workflows.
  - Added layout application for sidebar sections/width, bookmarks bar visibility, preview visibility/width, dual-pane mode, and file-list view mode.
  - Added smoke coverage for layout save/apply/delete, settings round-trip persistence, dialog loading, and XAML wiring.
- **Spec 016: Advanced Attribute/Date Search Filter** (New in this session)
  - Added an expandable advanced-search row with hidden/system/read-only/archive filters plus size and modified-date range inputs.
  - Updated recursive search to validate size/date ranges and combine advanced criteria with existing name and content search behavior.
  - Persisted advanced criteria through saved searches and added smoke coverage for live filtering, replay, and XAML wiring.
- **Spec 017: Group By and Manual Sorting** (New in this session)
  - Added `View > Group By` controls plus WPF group headers so Details and Tiles panes can group by name, type, size, or modified date.
  - Moved file-list sorting into `FileListViewModel`, including a manual-sort mode that supports drag reordering within the active folder.
  - Persisted per-folder manual order in the settings file and added smoke coverage for date-group labels plus manual-order reloads.
- **Spec 007: Bookmark Drag Feedback** (New in this session)
  - Added inline bookmark drag hint text covering folder, sibling, root, and invalid drop states.
  - Highlighted active bookmark drop targets and the bookmark-tree root surface during drag operations.
  - Centralized bookmark drop validation in `MainViewModel` and cleared transient drag state on drop/leave completion.
  - Updated smoke tests and XAML wiring checks to verify the new drag feedback behavior.
- **Roadmap: New-file Templates Support** (New in this session)
  - Added a configurable template-folder preference persisted in `UserSettings` and `SettingsService`.
  - Added template catalog loading in `MainViewModel` and dynamic `New` submenu population in the Edit menu and background pane context menus.
  - Added template-backed file creation through `FileSystemService.CreateFileFromTemplate` with undo/redo parity in `FileListViewModel`.
  - Updated smoke coverage for template catalog loading, template file creation undo/redo, settings round-trip, and XAML wiring.
- **Spec 008: App Icon Refresh (v2)** (New in this session)
  - Updated `CubicAIExplorer.csproj` to stamp the build output with `Resources\appicon-v2.ico`.
  - Updated `MainWindow.xaml` so the title bar and taskbar use the same v2 icon resource.
  - Added smoke coverage that verifies the project wiring and that the ICO contains 16/32/48/256 frames.
- **Spec 009: Empty Recycle Bin** (New in this session)
  - Added a `Tools > Empty Recycle Bin...` action with confirmation handled in `MainWindow`.
  - Added `IFileSystemService.EmptyRecycleBin()` with shell-backed `SHEmptyRecycleBinW` integration in `FileSystemService`.
  - Updated `MainViewModel` to expose the command and surface success/failure status text without embedding modal UI in the command path.
  - Added smoke coverage using the recording filesystem test double.
- **Spec 010: Shell Verb Execution** (New in this session)
  - Added `IFileSystemService.ExecuteShellVerb()` backed by `ShellExecuteEx` so alternate launches stay inside the shell-aware service boundary.
  - Added `Open in New Window` and `Run as Administrator` commands in `MainViewModel` with menu and pane-context-menu wiring in `MainWindow`.
  - Added smoke coverage that verifies the requested shell verb and target path without triggering real elevated prompts.
- **Spec 011: File Watcher Hardening** (New in this session)
  - Replaced the narrow last-write watcher callbacks in `SettingsService` and `BookmarkService` with a shared debounced watcher that listens for create/change/delete/rename events.
  - Added watcher recreation on `FileSystemWatcher.Error` and suppression around service-owned writes to avoid self-trigger loops.
  - Added smoke coverage for external settings delete/recreate and bookmark temp-file replacement flows.
- **Spec 006: Broader Preview Support**
  - Added rich text preview support using `FlowDocument` and `RichTextBox` for Markdown and Code.
  - Implemented a dependency-free Markdown renderer for bold, headers, and lists.
  - Implemented regex-based syntax highlighting for C#, XML, JSON, and Python.
  - Enhanced `UpdatePreview` to detect and route to rich previews for relevant extensions.
  - Updated smoke tests to verify rich preview properties and rendering.
- **Spec 005: Shell Property Exposure**
  - Implemented `ShellPropertyHelper` using `SHGetPropertyStoreFromParsingName` and `IPropertyStore` for robust metadata retrieval.
  - Added "Company", "Version", "Dimensions", and "Duration" columns to the Details view.
  - Updated `MainWindow` and `MainViewModel` to handle these new columns (toggling, sorting, persistence).
  - Enhanced the internal Properties dialog to display these shell properties in a dedicated details section.
  - Updated smoke tests to verify property retrieval and account for new default column set.
- Windows Shell context menu integration:
  - Added `UseShellContextMenu` property to `UserSettings` with a preference toggle in the UI.
  - Implemented `ShellContextMenuHelper` with support for `IContextMenu`, `IContextMenu2`, and `IContextMenu3` correctly via window subclassing.
  - Implemented "background" shell context menu when right-clicking empty space in the file list.
- Unified Reveal and Native Properties:
  - Unified `RevealInExplorer` to use the native `SHOpenFolderAndSelectItems` API.
  - Added `ShowNativeProperties` integration using `ShellExecuteEx` for the official Windows properties dialog.
- Crowded-tab affordances and tab management:
  - Added horizontal scroll, overflow menu, and close-left/right/other actions to tabs.
- Richer filter/search model:
  - Added explicit match modes (Contains, Wildcard, Exact) and saved search persistence.

## Next Steps

- No incomplete numbered specs remain in this checkout.
- Re-check `IMPLEMENTATION_PLAN.md`, `CONTINUE.md`, and any newly added specs before starting new roadmap work.

## Key Files

- `src/CubicAIExplorer/Models/FileOperationQueueHistoryEntry.cs`
- `src/CubicAIExplorer/Models/BatchRenameModels.cs`
- `src/CubicAIExplorer/Services/FileOperationQueueService.cs`
- `src/CubicAIExplorer/Services/BatchRenameService.cs`
- `src/CubicAIExplorer/Services/IFileOperationQueueService.cs`
- `src/CubicAIExplorer/MainWindow.xaml`
- `src/CubicAIExplorer/Services/DebouncedJsonFileWatcher.cs`
- `src/CubicAIExplorer/Models/FileChecksumSet.cs`
- `src/CubicAIExplorer/Models/DropStackItem.cs`
- `src/CubicAIExplorer/ViewModels/MainViewModel.cs`
- `src/CubicAIExplorer/ViewModels/FileListViewModel.cs`
- `src/CubicAIExplorer/Views/BatchRenameDialog.xaml`
- `src/CubicAIExplorer/Views/SplitFileDialog.xaml`
- `src/CubicAIExplorer/Views/JoinFileDialog.xaml`
- `src/CubicAIExplorer/Views/ChecksumDialog.xaml`
- `src/CubicAIExplorer/Models/NewFileTemplateItem.cs`
- `src/CubicAIExplorer/Models/ExternalTool.cs`
- `src/CubicAIExplorer/Models/UserSettings.cs`
- `src/CubicAIExplorer/PreferencesWindow.xaml`
- `src/CubicAIExplorer/MainWindow.xaml`
- `src/CubicAIExplorer/MainWindow.xaml.cs`
- `src/CubicAIExplorer/Models/BreadcrumbSegment.cs`
- `src/CubicAIExplorer/Models/BreadcrumbDropdownItem.cs`
- `src/CubicAIExplorer/Models/SavedSearchItem.cs`
- `tests/CubicAIExplorer.SmokeTests/Program.cs`
- `src/CubicAIExplorer/Services/ShellPropertyHelper.cs`
- `src/CubicAIExplorer/Services/FileSystemService.cs`

## Worktree

Tracked worktree state:

- Spec 020 code review fixes are the latest verified slice in this checkout.
- All numbered specs currently present in `specs/` are complete in this checkout.
- planning/history docs were refreshed to keep roadmap state aligned with the current implementation.

## Verification

Verification run on the updated checkout on 2026-03-12:

- `dotnet build CubicAIExplorer.sln -v minimal`
  - passed
- `dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal`
  - passed
- `tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe`
  - passed (all smoke tests pass, including advanced attribute/date search coverage, advanced saved-search replay, layout save/apply/delete coverage, split/join round-trips, checksum comparison coverage, external-tool launch coverage, drop-stack collection/transfer coverage, tab-lock fork navigation, bookmarks-bar visibility/drop persistence coverage, content-only search, breadcrumb dropdown coverage, sanitized env override coverage, JSON Id round-trips, and the forced symbolic-link failure regression)

## Gotchas

- **FlowDocument Performance**: For preview purposes, we limit Markdown rendering to the first 500 lines and source code highlighting to the first 64 KB to maintain UI responsiveness.
- **PROPVARIANT Size**: On 64-bit, the `PROPVARIANT` structure must be at least 24 bytes. My implementation uses `IntPtr` and `Marshal.AllocCoTaskMem(24)` for safety.
- **IShellItem2 Vtable**: When defining `IShellItem2`, ensure all methods are in the correct order (including the 3 methods between `GetPropertyStore` and `GetProperty`).
- **Smoke Test App State**: Creating a WPF `App` instance in a smoke test can have side effects on subsequent tests. Move app-dependent tests to the end if possible.
- **Watcher Semantics**: Cross-instance settings/bookmark sync now depends on the shared debounced watcher helper handling create/delete/rename/error events. Keep service-owned saves wrapped in watcher suppression to avoid self-triggered reloads.
- **Queue History Bound**: Recent file-operation history is intentionally capped to a small in-memory list so the status-bar popup stays readable; if you expand it later, keep it bounded and avoid modal failure reporting for queue-level summaries.
- **Headless Symbolic-Link Failures**: Keep symbolic-link privilege failures non-modal when `Application.Current?.MainWindow` is unavailable so Ralph/smoke runs can skip or assert on the exception instead of hanging on `MessageBox`.
- **Content Search Scope**: Content scanning is intentionally limited to a small text-extension allowlist and files at or below 10 MB; widen that list carefully if future specs ask for richer grep behavior.
- **Chunk Join Safety**: Join now requires a contiguous numeric chunk sequence starting from the selected `.001`-style file, so later work should preserve that validation rather than silently skipping gaps.
- **External Tool Arguments**: `%p` is the only placeholder currently expanded for external tools. If future work adds more tokens, keep the selected-file path quoted and continue routing process launch through `IFileSystemService`.
- **Drop Stack Lifetime**: The collected entries are intentionally session-local and not persisted. If future work extends the feature, decide explicitly whether stale-path cleanup or persistence is desirable before adding either.
