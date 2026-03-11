# Continuation Instructions for Next Session

> Last updated: 2026-03-11
> Branch: `master`
> HEAD: current local `master` after the latest verified roadmap slice
> Status: Windows Shell context menu integration, multi-select Explorer reveal, and native properties dialog support are now implemented and verified.

Continue in `C:\dev\CubicAI_rewrite` on `CubicAIExplorer.sln`.

## Status

- Local `master` contains the latest verified roadmap slices in this checkout.
- The branch builds and the smoke harness passes again.
- Specs `001-named-session-manager.md`, `002-richer-filter-search-model.md`, and `003-safer-file-operations.md` are complete in this checkout.
- The latest roadmap slice after those specs is REAL Windows Shell context menu integration for file lists.
- Remaining untracked paths are mostly local Ralph/tooling folders (`.claude/`, `.cursor/`, `.specify/`, `completion_log/`, `obj_verify/`, helper scripts).

## Completed

- Windows Shell context menu integration:
  - Added `UseShellContextMenu` property to `UserSettings` with a preference toggle in the UI.
  - Implemented `ShellContextMenuHelper` with support for `IContextMenu`, `IContextMenu2`, and `IContextMenu3` to handle submenus (e.g., "Send To", "Open With") correctly via window subclassing.
  - Added shell context menu support to `FileListView`, `FolderTree`, and `BookmarkTree` (for filesystem bookmarks).
  - Updated `MainWindow` to intercept context menu events and show the native menu when enabled.
- Unified Reveal and Native Properties:
  - Unified `RevealInExplorer` to use the native `SHOpenFolderAndSelectItems` API for both single and multiple selections, providing a more consistent and robust experience than calling `explorer.exe /select`.
  - Added `ShowNativeProperties` integration using `ShellExecuteEx` with `SEE_MASK_INVOKEIDLIST` to host the official Windows properties dialog.
  - Updated `MainWindow` and `MainViewModel` to prefer the native properties dialog when shell integration is enabled, covering file list items and bookmarks.
  - Modernized `OpenInDefaultApp` to use `ShellExecute` directly on paths for standard folder and file launches.
- Multi-select Explorer reveal:
  - Updated `Open in Explorer` command to use a single shell API call for highlighting all selected items.
  - Smoke coverage now verifies multi-selection reveal logic.
- Crowded-tab affordances:
  - added persisted `NamedSession` records inside `UserSettings`
  - added File > Sessions UI for save-as, update, load, delete, and startup-session selection
  - startup now restores a configured named session before falling back to generic last-state restore
  - smoke coverage now verifies named session save/load/delete/startup behavior
- Richer filter/search model:
  - added `Contains`, `Wildcard`, and `Exact` match modes for inline filters and recursive search
  - saved searches now persist and replay their chosen match mode
  - filter history now persists through settings and is reusable from the main window
  - users can opt into clearing inline filters automatically when changing folders
- Details-column customization:
  - added persisted details-column settings for width, visibility, and order in `UserSettings`
  - the View menu now exposes column show/hide, move-left/right, auto-size, and reset actions
  - both panes rebuild the details view from the saved layout so settings survive restart and view-mode switches
  - smoke coverage now verifies column-layout defaults, normalized saves, and settings-service round-trip persistence
- Tab-management parity:
  - added tab context-menu actions for close tabs to the left and close tabs to the right
  - shared close logic now detaches tab event subscriptions consistently for close-left/right/others flows
  - when a close-left/right action removes the active tab, the clicked tab becomes the active fallback
  - smoke coverage now verifies close-left/right behavior
- Tab reuse follow-up:
  - bookmark `Open in New Tab` now activates an existing tab when that folder is already open
  - bookmark `Open All in Tabs` now only creates tabs for unopened folders and reuses existing ones
  - smoke coverage now verifies both single-bookmark and category open-all reuse flows
- Crowded-tab affordances:
  - the main tab strip now scrolls horizontally when there are more tabs than fit in the window
  - added left/right tab-strip scroll buttons plus a `More Tabs` dropdown listing every open tab
  - active-tab changes now auto-scroll the selected tab back into view after tab-count or window-size changes
  - smoke coverage now verifies the overflow wiring is present in the main window
- Shell-aware display names and known-folder aliases:
  - tab titles, breadcrumbs, recent folders, and new bookmark labels now use Windows shell display names instead of raw path parsing where available
  - the address bar now resolves common aliases such as `Desktop`, `Documents`, `Downloads`, `Pictures`, `Music`, `Videos`, and `Home`
  - address autocomplete now suggests matching known folders in addition to normal filesystem completions
  - smoke coverage now verifies alias navigation, shell display-name routing, and known-folder suggestions
- Shell-backed type metadata:
  - centralized shell file-info lookup so Windows-reported type names now feed the Details `Type` column, preview header, recursive search results, and properties dialog
  - bookmark properties now populate real timestamps, attributes, size, and shell type labels instead of placeholder defaults
  - smoke coverage now verifies shell-backed type descriptions in directory listings, recursive search results, and the properties dialog
- Explorer reveal behavior:
  - `Open in Explorer` now reveals a single selected file or folder instead of always opening the containing folder generically
  - multi-selection now uses the Windows shell selection API so Explorer highlights every selected item from the current folder
  - no-selection flows still open the current folder to avoid ambiguous behavior
  - shell launch logic now routes through `IFileSystemService` so the behavior is testable and stays out of window code-behind
  - smoke coverage now verifies both single-selection and multi-selection reveal behavior
- Bookmark management refactoring:
  - refactored bookmark management into a dedicated `BookmarkService` to share robust persistence logic with `SettingsService`.
  - implemented reliable sync with automatic retries and hardened `FileSystemWatcher` callbacks to handle transient file locks.
  - organized bookmark data access into an atomic load/save pattern with watcher suspension during local writes.
  - updated `MainViewModel` and `App.xaml.cs` to use the new service.
- Expanded smoke-test coverage:
  - added verification for same-folder `Duplicate` behavior and its undo/redo path.
  - added coverage for risky file-operation paths, specifically verifying backup restoration after failed `Replace` transfers for both files and directories.
  - verified undo/redo for `New File` and `Create Symbolic Link` operations.
  - hardened the smoke harness to support multi-window tests without process shutdown races by setting `ShutdownMode.OnExplicitShutdown`.

## Next Steps

1. Continue deeper shell integration.
   - implement "background" shell context menu when right-clicking empty space in the file list
   - review remaining Explorer interop edge cases beyond reveal/select behavior
2. UX polish and advanced operations:

   - add broader preview type support (e.g., more image formats, syntax highlighting for text)
   - improve bookmark drag/drop feedback and visual cues
   - add new-file templates support (parity with original CubicExplorer)
3. Infrastructure and reliability:
   - further harden `FileSystemWatcher` callbacks across all services
   - improve error reporting in the file operation queue history

## Key Files

- `src/CubicAIExplorer/Services/BookmarkService.cs`
- `src/CubicAIExplorer/Services/SettingsService.cs`
- `src/CubicAIExplorer/ViewModels/MainViewModel.cs`
- `src/CubicAIExplorer/ViewModels/FileListViewModel.cs`
- `tests/CubicAIExplorer.SmokeTests/Program.cs`

## Worktree

Tracked worktree state:

- Explorer reveal-with-selection behavior and its smoke coverage are local and uncommitted in this checkout
- planning/history/spec docs were refreshed to keep roadmap state aligned with the current implementation

Untracked local-only paths:

- `.claude/`
- `.cursor/`
- `.specify/`
- `completion_log/`
- `history/`
- `scripts/lib/`
- `scripts/ralph-loop-codex.ps1`
- `scripts/ralph-loop-codex.sh`
- `scripts/ralph-loop-copilot.sh`
- `scripts/ralph-loop-gemini.sh`
- `scripts/ralph-loop.sh`
- `specs/`
- `src/CubicAIExplorer/obj_verify/`
- `tests/CubicAIExplorer.SmokeTests/obj_verify/`

## Verification

Verification run on the updated checkout on 2026-03-11:

- `dotnet build CubicAIExplorer.sln -v minimal`
  - passed
- `dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal`
  - passed
- `tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe`
  - passed

## Gotchas

- WPF markup generation can intermittently lock `App.g.cs` or `CubicAIExplorer_MarkupCompile.cache`; rerun the build if that happens.
- Do not recreate the `GridView` during tab initialization.
- Avoid keyed `DataTemplate` plus `DataType` combinations in `App.xaml`.
- Do not enable WinForms just to get a folder picker.
- Keep all path handling routed through `FileSystemService` sanitization helpers.
