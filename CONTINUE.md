# Continuation Instructions for Next Session

> **Last updated:** 2026-03-09
> **Status:** Tier 1 complete + dual-pane, preview, autocomplete, and preferences UI all implemented. Settings/preferences feature is committed with smoke coverage. New uncommitted follow-up: active-pane proxy cleanup (direct `CurrentPaneFileList.*` bindings).

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
- Preview panel with async loading, folder preview, and file metadata fallback
- Address bar autocomplete with drive-root completion and debounced async suggestions

### Dual-pane parity
- Active-pane model for commands and navigation
- Right-pane context menu parity
- Right-pane sorting, inline rename, select-all, properties routing
- Shared filter/search/view-mode controls routed to the active pane
- Active-pane status labels and visual highlighting
- Current-pane navigation from breadcrumbs, recent folders, bookmarks, tree selection, and autocomplete
- Right-pane header single-click activation and inline address editing with autocomplete

### Async I/O
- Preview loading (images, text, folders) runs on background threads with generation tracking
- Address suggestions debounced 100ms + filesystem queries on background thread
- `SaveRecentFolders` uses `File.WriteAllTextAsync`

## Current Worktree State

**Committed locally:**
- `8ad16c9` — async preview, drive-root autocomplete, debounced suggestions, right-pane autocomplete
- `6445720` — preferences/settings persistence + smoke coverage

**Uncommitted but building + tests passing — proxy cleanup follow-up:**
- `src/CubicAIExplorer/ViewModels/MainViewModel.cs` — removed `Current*` proxy properties that forwarded to `CurrentPaneFileList`, plus related manual `OnPropertyChanged` forwarding
- `src/CubicAIExplorer/MainWindow.xaml` — bindings switched to direct `CurrentPaneFileList.*` for filter/search/show-hidden/search-results state
- `src/CubicAIExplorer/MainWindow.xaml.cs` — filter clear and view mode set now target `CurrentPaneFileList` directly
- `tests/CubicAIExplorer.SmokeTests/Program.cs` — updated active-pane routing and XAML wiring assertions for direct bindings

Untracked local-only paths:
- `.claude/`
- `src/CubicAIExplorer/obj_verify/`
- `tests/CubicAIExplorer.SmokeTests/obj_verify/`

## Verification
Verified on **2026-03-09**:
- `dotnet build CubicAIExplorer.sln` — 0 errors, 0 warnings
- Smoke tests: 41/41 pass (includes settings defaults + round-trip + `NewTab` settings application)

## Priority Next Work
1. **Commit proxy cleanup follow-up** (direct `CurrentPaneFileList.*` bindings and removed `Current*` forwarding properties).
2. **Expand preview to more file types.**
   PDF metadata, audio/video info would add value but may require new NuGet packages.
3. **Keyboard accessibility polish.**
   Tab order, focus management, and keyboard-only navigation through all panels.

## Known Gotchas
- **WPF markup build lock:** smoke-test project builds can fail in the sandbox with `App.g.cs` / `MarkupCompile.cache` access-denied errors under `src\CubicAIExplorer\obj\Debug\net8.0-windows`. Re-running the smoke-test build outside the sandbox resolves it.
- **View mode bug (fixed):** `ApplyViewMode("Details")` must not run during tab initialization, or it replaces the XAML-defined `GridView` and breaks rendering.
- **DockPanel LastChildFill:** wrapping a file list and popup in a `DockPanel` causes layout issues. Use a `Grid` with explicit rows.
- **KeyBinding null commands:** WPF `KeyBinding` requires a non-null command.
- **Keyed DataTemplate + DataType:** avoid combining them in `App.xaml`.
- **Right-pane header double-click:** `Border` does not support a `MouseDoubleClick` XAML event. Handle double-click via `MouseLeftButtonDown` and `ClickCount`.
- **Forwarding commands:** MainViewModel has ~20 `[RelayCommand]` methods that forward to `CurrentPaneFileList?.XCommand.Execute(null)`. These exist because XAML KeyBindings and toolbar buttons bind to MainViewModel. Could be eliminated by binding directly to `CurrentPaneFileList.*` in XAML.
- **WinForms namespace collisions:** Adding `<UseWindowsForms>true</UseWindowsForms>` to the csproj causes widespread `Point`, `Brush`, `DragEventArgs`, `ListView` ambiguities. Use `Microsoft.Win32.OpenFolderDialog` (.NET 8+) instead of `System.Windows.Forms.FolderBrowserDialog`.

## Notes
- No new NuGet packages unless explicitly approved.
- Keep all file paths sanitized through `FileSystemService`.
- Root markdown files (`CLAUDE.md`, `CONTINUE.md`, `IMPLEMENTATION_PLAN.md`) should stay in sync with the actual repo state.

---
