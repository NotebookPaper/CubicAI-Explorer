# Continuation Instructions for Next Session

> Last updated: 2026-03-11
> Branch: `master`
> HEAD: current local `master` after file watcher hardening
> Status: File watcher hardening for settings/bookmark sync is implemented and verified.

Continue in `C:\dev\CubicAI_rewrite` on `CubicAIExplorer.sln`.

## Status

- Local `master` contains the latest verified roadmap slices in this checkout.
- The branch builds and the smoke harness passes, including watcher hardening coverage.
- Specs `001`, `002`, `003`, `004`, `005`, `006`, `007`, `008`, `009`, `010`, and `011` are complete in this checkout.
- The top roadmap item after the numbered specs, new-file templates support, is also complete in this checkout.
- Remaining untracked paths are mostly local Ralph/tooling folders (`.claude/`, `.cursor/`, `.specify/`, `completion_log/`, `obj_verify/`, helper scripts).

## Completed

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

3. Infrastructure and reliability:
   - improve error reporting in the file operation queue history

## Key Files

- `src/CubicAIExplorer/Services/DebouncedJsonFileWatcher.cs`
- `src/CubicAIExplorer/ViewModels/MainViewModel.cs`
- `src/CubicAIExplorer/Models/NewFileTemplateItem.cs`
- `src/CubicAIExplorer/PreferencesWindow.xaml`
- `src/CubicAIExplorer/MainWindow.xaml`
- `src/CubicAIExplorer/MainWindow.xaml.cs`
- `tests/CubicAIExplorer.SmokeTests/Program.cs`
- `src/CubicAIExplorer/Services/ShellPropertyHelper.cs`
- `src/CubicAIExplorer/Services/FileSystemService.cs`

## Worktree

Tracked worktree state:

- Spec 011 file watcher hardening is the latest completed roadmap slice in this checkout.
- New-file templates support, app icon refresh, recycle-bin management, and shell-verb execution remain complete in this checkout.
- planning/history/spec docs were refreshed to keep roadmap state aligned with the current implementation.

## Verification

Verification run on the updated checkout on 2026-03-11:

- `dotnet build CubicAIExplorer.sln -v minimal`
  - passed
- `dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal`
  - passed
- `tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe`
  - passed (all smoke tests pass, including settings/bookmark watcher coverage)

## Gotchas

- **FlowDocument Performance**: For preview purposes, we limit Markdown rendering to the first 500 lines and source code highlighting to the first 64 KB to maintain UI responsiveness.
- **PROPVARIANT Size**: On 64-bit, the `PROPVARIANT` structure must be at least 24 bytes. My implementation uses `IntPtr` and `Marshal.AllocCoTaskMem(24)` for safety.
- **IShellItem2 Vtable**: When defining `IShellItem2`, ensure all methods are in the correct order (including the 3 methods between `GetPropertyStore` and `GetProperty`).
- **Smoke Test App State**: Creating a WPF `App` instance in a smoke test can have side effects on subsequent tests. Move app-dependent tests to the end if possible.
- **Watcher Semantics**: Cross-instance settings/bookmark sync now depends on the shared debounced watcher helper handling create/delete/rename/error events. Keep service-owned saves wrapped in watcher suppression to avoid self-triggered reloads.
