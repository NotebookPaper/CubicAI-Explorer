# Continuation Instructions for Next Session

> **Last updated:** 2026-03-09
> **Status:** Tier 1 complete + dual-pane parity/polish well beyond MVP. Latest code is committed and pushed on `origin/master`. Smoke suite passes. Recent refactor cleaned up event leaks and code duplication.

---

You are continuing work on **CubicAI Explorer**, a C#/WPF file manager rewrite.

**Working directory:** `C:\dev\CubicAI_rewrite`
**Solution:** `CubicAIExplorer.sln`
**Build:** `dotnet build CubicAIExplorer.sln`
**Run:** `dotnet run --project src/CubicAIExplorer/CubicAIExplorer.csproj`
**Smoke tests:** `dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal` then run `tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe`

## Current Implemented State

### Tier 1 (complete)
- File operations: copy/move/delete/permanent delete/rename/create folder
- Clipboard interop with Windows Explorer (`CF_HDROP` + `Preferred DropEffect`)
- Shell icons via `SHGetFileInfo` + converter
- Context menu, keyboard shortcuts, multi-select
- Inline rename (F2)

### Post-Tier-1 work now complete
- Bookmarks panel with persistence to `%AppData%\\CubicAIExplorer\\bookmarks.json`
- File list drag/drop copy/move
- Undo/redo history for rename/copy/move/new-folder/permanent-delete
- Clear History command
- Same-folder move guard
- Classic Cubic/XP-inspired theme pass
- Full toolbar with Cut/Copy/Paste/Delete/Undo/Redo/Refresh buttons
- View mode switching (Details/List/Tiles)
- Search/filter bar with recursive search
- File properties dialog
- Drag/drop files to folder tree nodes
- Sort indicator arrows on column headers
- Tab context menu (Duplicate, Close, Close Other Tabs)
- Status bar selection count and total selected size
- Open in Explorer
- Enter key opens selected item
- Window size/position persistence
- Breadcrumb-style address bar
- Recent folders panel
- Toolbar vector icons replacing Unicode glyphs
- Address bar autocomplete suggestions
- Dual-pane mode with active-pane routing
- Preview panel for text/images with async loading, folder preview, and file metadata fallback
- Address bar autocomplete with drive-root completion and debounced async suggestions

### Dual-pane parity already implemented
- Active-pane model for commands and navigation
- Right-pane context menu parity
- Right-pane sorting, inline rename, select-all, properties routing
- Shared filter/search/view-mode controls routed to the active pane
- Active-pane status labels and visual highlighting
- Current-pane navigation from breadcrumbs, recent folders, bookmarks, tree selection, and autocomplete
- Right-pane header single-click activation and inline address editing with autocomplete

### Recent refactor (4442153)
- Fixed event listener leak: closed tabs now properly unsubscribed via `DetachTab()`
- Fixed right-pane drop bug: subfolder drop targeting now works (was always dropping to current dir)
- Deduplicated left/right pane handlers into shared methods (`SyncSelection`, `TryInitiateDrag`, `HandleDrop`, `OpenSelectedInPane`, `ConfigureContextMenu`, `HookPaneFileListViewModel`)
- Cached `LinearGradientBrush` as static frozen field
- Removed duplicate `UpdatePreview` calls on selection change
- Consolidated `OnFileListPropertyChanged` to use switch + cached `isCurrentPane`
- Consolidated `OnTabPropertyChanged` to single `CurrentPath` check
- Extracted `GetAppDataPath()` and `GetDisplayName()` helpers

## Current Worktree State
Tracked files are committed through `4442153` on `origin/master`.

Untracked local-only paths still present:
- `.claude/`
- `src/CubicAIExplorer/obj_verify/`
- `tests/CubicAIExplorer.SmokeTests/obj_verify/`

## Verification
Verified on **2026-03-09**:
- `dotnet build CubicAIExplorer.sln`
- `dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal`
- `tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe`

Current smoke coverage includes:
- dual-pane toggle
- active pane command routing
- active pane UI command routing
- active pane view/search routing
- active pane status labels
- current pane navigation routing
- current pane navigation sources
- preview properties
- preview refresh on tab switch
- preview status states
- address suggestions
- XAML wiring checks

## Priority Next Work
1. **Consider eliminating proxy properties.**
   MainViewModel has ~15 `Current*` proxy properties that forward to `CurrentPaneFileList`. XAML could bind directly to `CurrentPaneFileList.FilterText` etc., eliminating manual `OnPropertyChanged` propagation.
2. **Expand preview to more file types.**
   PDF metadata, audio/video info, or rich text rendering would add value but may require new NuGet packages.
3. **Keyboard accessibility polish.**
   Tab order, focus management, and keyboard-only navigation through all panels.
4. **Settings/preferences UI.**
   Persist user preferences (default view mode, show hidden files, startup folder, etc.).

## Known Gotchas
- **WPF markup build lock:** smoke-test project builds can fail in the sandbox with `App.g.cs` / `MarkupCompile.cache` access-denied errors under `src\CubicAIExplorer\obj\Debug\net8.0-windows`. Re-running the smoke-test build outside the sandbox resolves it.
- **View mode bug (fixed):** `ApplyViewMode("Details")` must not run during tab initialization, or it replaces the XAML-defined `GridView` and breaks rendering.
- **DockPanel LastChildFill:** wrapping a file list and popup in a `DockPanel` causes layout issues. Use a `Grid` with explicit rows.
- **KeyBinding null commands:** WPF `KeyBinding` requires a non-null command.
- **Keyed DataTemplate + DataType:** avoid combining them in `App.xaml`.
- **Right-pane header double-click:** `Border` does not support a `MouseDoubleClick` XAML event. Handle double-click via `MouseLeftButtonDown` and `ClickCount`.
- **Forwarding commands:** MainViewModel has ~20 `[RelayCommand]` methods that forward to `CurrentPaneFileList?.XCommand.Execute(null)`. These exist because XAML KeyBindings and toolbar buttons bind to MainViewModel. Could be eliminated by binding directly to `CurrentPaneFileList.*` in XAML.

## Notes
- No new NuGet packages unless explicitly approved.
- Keep all file paths sanitized through `FileSystemService`.
- Root markdown files (`CLAUDE.md`, `CONTINUE.md`, `IMPLEMENTATION_PLAN.md`) should stay in sync with the actual repo state.

---
