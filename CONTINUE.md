# Continuation Instructions for Next Session

> **Last updated:** 2026-03-09
> **Status:** Tier 1 complete + post-Tier-1 enhancements through dual-pane/preview/address-autocomplete MVP. Current uncommitted batch builds and smoke tests pass.

---

You are continuing work on **CubicAI Explorer**, a C#/WPF file manager rewrite.

**Working directory:** `C:\dev\CubicAI_rewrite`  
**Solution:** `CubicAIExplorer.sln`  
**Build:** `dotnet build CubicAIExplorer.sln`  
**Run:** `dotnet run --project src/CubicAIExplorer/CubicAIExplorer.csproj`  
**Smoke tests:** `dotnet run --project tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj`

## Current Implemented State

### Tier 1 (complete)
- File operations: copy/move/delete/permanent delete/rename/create folder
- Clipboard interop with Windows Explorer (`CF_HDROP` + `Preferred DropEffect`)
- Shell icons via `SHGetFileInfo` + converter
- Context menu, keyboard shortcuts, multi-select
- Inline rename (F2)

### Additional features completed
- Bookmarks panel (add/remove/navigate)
- Bookmark persistence to `%AppData%\\CubicAIExplorer\\bookmarks.json`
- File list drag/drop copy/move
- Undo/redo history:
  - undo/redo rename
  - undo/redo copy
  - undo/redo move
  - undo new folder
  - in-session undo for permanent delete via staging folder
- Clear History command
- Same-folder move guard (no-op to avoid accidental duplicate renames)
- Classic Cubic/XP-inspired visual theme pass
- Bookmark single-click navigation (selection change triggers navigation)
- Full toolbar with Cut/Copy/Paste/Delete/Undo/Redo/Refresh buttons
- View mode switching (Details/List/Tiles) via View menu
- Selection count displayed in status bar ("25 items | 3 selected")
- Search/filter bar (Ctrl+F) to filter files by name in current directory
- File properties dialog (Alt+Enter or context menu → Properties)
- Drag/drop files onto folder tree nodes (copy/move)
- Sort indicator arrows (▲/▼) on column headers
- Tab context menu: Duplicate Tab, Close Tab, Close Other Tabs
- Status bar shows total size of selected files
- Open in Explorer from context menu
- Enter key opens selected item when file list is focused
- Window size/position persisted to `%AppData%\CubicAIExplorer\window.json`
- Breadcrumb-style address bar
- Recent folders panel
- Recursive search in current folder tree
- Toolbar vector icons replacing Unicode toolbar glyphs
- Address bar autocomplete suggestions
- Dual-pane mode (MVP)
- Preview panel for text and common image files

## Uncommitted Changes
There is an uncommitted batch across 4 modified files plus untracked `.claude/` workspace metadata.
These changes build cleanly, and the smoke-test suite passes end-to-end.
**The next session should commit the app/test changes before starting a new feature slice.**

Modified files:
- `src/CubicAIExplorer/MainWindow.xaml` — dual-pane layout, preview panel, address autocomplete popup, F6/F7 bindings
- `src/CubicAIExplorer/MainWindow.xaml.cs` — autocomplete interactions, preview refresh hook, dual-pane/preview column toggles, right-pane drag/drop handlers
- `src/CubicAIExplorer/ViewModels/MainViewModel.cs` — dual-pane state, preview state, address suggestions
- `tests/CubicAIExplorer.SmokeTests/Program.cs` — smoke coverage for dual-pane toggle, preview state, address suggestions

## Recent Commits (latest first)
- `00c0217` feat: add redo/history controls and classic cubic theming
- `a2c4916` feat: support undo for permanent delete via staging
- `1032c0f` feat: extend undo with copy rollback and action label
- `4fd06ac` feat: add basic undo history for rename and move
- `73a357c` feat: add file list drag-and-drop copy/move
- `77396c7` feat: persist bookmarks to appdata json
- `2523401` feat: add bookmarks mvp with UI and commands
- `f6e5967` feat: add inline rename and tier1 smoke tests

## Priority Next Work
1. Complete dual-pane parity:
   - context menu and keyboard command parity in the right pane
   - active-pane focus model for copy/cut/paste/delete/navigation
   - sorting, inline rename, and selection-status parity
2. Expand preview support:
   - empty/error states
   - more file-type coverage
   - avoid loading very large files synchronously
3. Polish address autocomplete:
   - richer keyboard navigation
   - better completion for roots and partial drive paths
4. Commit the current batch, then choose the next UX polish slice

## Known Gotchas
- **View mode bug (fixed):** `ApplyViewMode("Details")` must NOT be called during tab initialization — it replaces the XAML-defined GridView and breaks item rendering. The fix: only call `ApplyViewMode` when the mode is not "Details" (the XAML default). See `HookFileListViewModel` in `MainWindow.xaml.cs`.
- **DockPanel LastChildFill:** Don't use DockPanel to wrap the file list + Popup — the Popup steals fill space. Use a Grid with `Auto`/`*` rows instead.
- **`{x:Null}` keybindings:** WPF KeyBinding requires a non-null Command. Never use `Command="{x:Null}"` — it silently breaks the window.
- **DataType on keyed DataTemplates:** Don't add `DataType="{x:Type ...}"` to keyed DataTemplates in App.xaml — it can interfere with implicit template resolution.

## Notes
- Verified on 2026-03-09:
  - `dotnet build CubicAIExplorer.sln`
  - `dotnet run --project tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj`
- No new NuGet packages unless explicitly approved.
- Keep all file paths sanitized through `FileSystemService`.
- Root markdown files (`CLAUDE.md`, `CONTINUE.md`, `IMPLEMENTATION_PLAN.md`) should be updated when status changes.

---
