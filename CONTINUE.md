# Continuation Instructions for Next Session

> Last updated: 2026-03-11
> Branch: `master`
> HEAD: current local `master` after Spec 005
> Status: Shell property exposure (IPropertyStore) for details columns and Properties dialog is implemented and verified.

Continue in `C:\dev\CubicAI_rewrite` on `CubicAIExplorer.sln`.

## Status

- Local `master` contains the latest verified roadmap slices in this checkout.
- The branch builds and the smoke harness passes (with Spec 005 coverage).
- Specs `001`, `002`, `003`, `004`, and `005` are complete in this checkout.
- Remaining untracked paths are mostly local Ralph/tooling folders (`.claude/`, `.cursor/`, `.specify/`, `completion_log/`, `obj_verify/`, helper scripts).

## Completed

- **Spec 005: Shell Property Exposure** (New in this session)
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
   - add broader preview type support (e.g., syntax highlighting for code, Markdown rendering)
   - improve bookmark drag/drop feedback and visual cues
   - add new-file templates support (parity with original CubicExplorer)
2. Deeper shell integration (continued):
   - explore recycle bin management (empty recycle bin from app)
   - shell execution with different verbs (Run as administrator, etc.)
3. Infrastructure and reliability:
   - further harden `FileSystemWatcher` callbacks across all services
   - improve error reporting in the file operation queue history

## Key Files

- `src/CubicAIExplorer/Services/ShellPropertyHelper.cs`
- `src/CubicAIExplorer/Services/FileSystemService.cs`
- `src/CubicAIExplorer/Models/FileSystemItem.cs`
- `src/CubicAIExplorer/Models/ShellProperties.cs`
- `src/CubicAIExplorer/Views/PropertiesDialog.xaml.cs`
- `src/CubicAIExplorer/ViewModels/MainViewModel.cs`
- `tests/CubicAIExplorer.SmokeTests/Program.cs`

## Worktree

Tracked worktree state:

- Shell property exposure, context menu integration, and reveal-with-selection behavior are local and uncommitted.
- planning/history/spec docs were refreshed to keep roadmap state aligned with the current implementation.

## Verification

Verification run on the updated checkout on 2026-03-11:

- `dotnet build CubicAIExplorer.sln -v minimal`
  - passed
- `dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal`
  - passed
- `tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe`
  - passed (all 85+ tests pass, including shell properties)

## Gotchas

- **PROPVARIANT Size**: On 64-bit, the `PROPVARIANT` structure must be at least 24 bytes. My implementation uses `IntPtr` and `Marshal.AllocCoTaskMem(24)` for safety.
- **IShellItem2 Vtable**: When defining `IShellItem2`, ensure all methods are in the correct order (including the 3 methods between `GetPropertyStore` and `GetProperty`).
- **Smoke Test App State**: Creating a WPF `App` instance in a smoke test can have side effects on subsequent tests. Move app-dependent tests to the end if possible.
