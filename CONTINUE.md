# Continuation Instructions for Next Session

> Last updated: 2026-03-11
> Branch: `master`
> HEAD: current local `master` after new-file templates support
> Status: New-file templates support is implemented and verified.

Continue in `C:\dev\CubicAI_rewrite` on `CubicAIExplorer.sln`.

## Status

- Local `master` contains the latest verified roadmap slices in this checkout.
- The branch builds and the smoke harness passes (with Spec 007 coverage).
- Specs `001`, `002`, `003`, `004`, `005`, `006`, and `007` are complete in this checkout.
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

1. UX polish and advanced operations:
2. Deeper shell integration (continued):
   - explore recycle bin management (empty recycle bin from app)
   - shell execution with different verbs (Run as administrator, etc.)
3. Infrastructure and reliability:
   - further harden `FileSystemWatcher` callbacks across all services
   - improve error reporting in the file operation queue history

## Key Files

- `src/CubicAIExplorer/ViewModels/MainViewModel.cs`
- `src/CubicAIExplorer/Models/NewFileTemplateItem.cs`
- `src/CubicAIExplorer/PreferencesWindow.xaml`
- `src/CubicAIExplorer/MainWindow.xaml`
- `tests/CubicAIExplorer.SmokeTests/Program.cs`
- `src/CubicAIExplorer/Services/ShellPropertyHelper.cs`
- `src/CubicAIExplorer/Services/FileSystemService.cs`

## Worktree

Tracked worktree state:

- Spec 007 bookmark drag feedback is the latest completed roadmap slice in this checkout.
- New-file templates support is the latest completed roadmap slice in this checkout.
- planning/history/spec docs were refreshed to keep roadmap state aligned with the current implementation.

## Verification

Verification run on the updated checkout on 2026-03-11:

- `dotnet build CubicAIExplorer.sln -v minimal`
  - passed
- `dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal`
  - passed
- `tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe`
  - passed (all 90+ tests pass, including new-file templates coverage)

## Gotchas

- **FlowDocument Performance**: For preview purposes, we limit Markdown rendering to the first 500 lines and source code highlighting to the first 64 KB to maintain UI responsiveness.
- **PROPVARIANT Size**: On 64-bit, the `PROPVARIANT` structure must be at least 24 bytes. My implementation uses `IntPtr` and `Marshal.AllocCoTaskMem(24)` for safety.
- **IShellItem2 Vtable**: When defining `IShellItem2`, ensure all methods are in the correct order (including the 3 methods between `GetPropertyStore` and `GetProperty`).
- **Smoke Test App State**: Creating a WPF `App` instance in a smoke test can have side effects on subsequent tests. Move app-dependent tests to the end if possible.
