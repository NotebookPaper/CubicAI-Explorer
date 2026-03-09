# Continuation Instructions for Next Session

> **Last updated:** 2026-03-09
> **Status:** Tier 1 complete + dual-pane parity/polish is well beyond MVP. Latest tracked work is committed on `origin/master` and the smoke suite passes.

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
- Preview panel for text/common image files plus empty/error/size-limit states

### Dual-pane parity already implemented
- Active-pane model for commands and navigation
- Right-pane context menu parity
- Right-pane sorting, inline rename, select-all, properties routing
- Shared filter/search/view-mode controls routed to the active pane
- Active-pane status labels and visual highlighting
- Current-pane navigation from breadcrumbs, recent folders, bookmarks, tree selection, and autocomplete
- Right-pane header single-click activation and inline address editing

## Current Worktree State
Tracked files are committed through `02deb1c` on `origin/master`.

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
1. Expand preview support.
   Next likely value: more file types, async loading, and better image/text fallbacks.
2. Improve address autocomplete.
   The obvious gaps are root-drive completion, keyboard selection polish, and clearer completion behavior.
3. Consider whether the left pane should also get a clearer inline address-edit affordance to match the new right-pane workflow.
4. Commit the current batch before starting another feature slice.

## Known Gotchas
- **WPF markup build lock:** smoke-test project builds can fail in the sandbox with `App.g.cs` / `MarkupCompile.cache` access-denied errors under `src\CubicAIExplorer\obj\Debug\net8.0-windows`. Re-running the smoke-test build outside the sandbox resolves it.
- **View mode bug (fixed):** `ApplyViewMode("Details")` must not run during tab initialization, or it replaces the XAML-defined `GridView` and breaks rendering.
- **DockPanel LastChildFill:** wrapping a file list and popup in a `DockPanel` causes layout issues. Use a `Grid` with explicit rows.
- **KeyBinding null commands:** WPF `KeyBinding` requires a non-null command.
- **Keyed DataTemplate + DataType:** avoid combining them in `App.xaml`.
- **Right-pane header double-click:** `Border` does not support a `MouseDoubleClick` XAML event. Handle double-click via `MouseLeftButtonDown` and `ClickCount`.

## Notes
- No new NuGet packages unless explicitly approved.
- Keep all file paths sanitized through `FileSystemService`.
- Root markdown files (`CLAUDE.md`, `CONTINUE.md`, `IMPLEMENTATION_PLAN.md`) should stay in sync with the actual repo state.

---
