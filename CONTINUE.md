# Continuation Instructions for Next Session

> **Last updated:** 2026-03-08
> **Status:** Tier 1 complete + 22 post-Tier-1 enhancements. Uncommitted batch ready (visually verified working).

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

## Uncommitted Changes
There is a large batch of uncommitted work across 7 modified files + 2 new files.
These changes build cleanly (0 errors, 0 warnings), all 24 smoke tests pass, and the UI has been **visually verified working** by the user.
**The next session should commit these changes before starting new work.**

Modified files:
- `src/CubicAIExplorer/App.xaml` — keyed DataTemplates for view modes, WrapPanel template
- `src/CubicAIExplorer/App.xaml.cs` — window bounds persistence (save/restore)
- `src/CubicAIExplorer/MainWindow.xaml` — toolbar buttons, filter bar, tab context menu, tree drop, context menu additions
- `src/CubicAIExplorer/MainWindow.xaml.cs` — view mode switching, filter, properties, tree drop, tab context menu, sort arrows, Enter/Ctrl+F keys, Open in Explorer
- `src/CubicAIExplorer/ViewModels/FileListViewModel.cs` — filter, view mode, properties event, selection size status
- `src/CubicAIExplorer/ViewModels/MainViewModel.cs` — DuplicateTab, CloseOtherTabs, StatusText sync
- `tests/CubicAIExplorer.SmokeTests/Program.cs` — 5 new tests (filter, properties, duplicate tab, close others, selection size)

New files:
- `src/CubicAIExplorer/Views/PropertiesDialog.xaml` + `.cs` — file properties dialog

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
1. Increase visual fidelity to original CubicExplorer:
   - classic toolbar icon assets (replace Unicode symbols with 16x16 bitmap icons)
   - pane/tab spacing and border polish
2. Breadcrumb-style address bar (clickable path segments)
3. File search across subdirectories (recursive search)
4. Quick access / recent folders panel

## Known Gotchas
- **View mode bug (fixed):** `ApplyViewMode("Details")` must NOT be called during tab initialization — it replaces the XAML-defined GridView and breaks item rendering. The fix: only call `ApplyViewMode` when the mode is not "Details" (the XAML default). See `HookFileListViewModel` in `MainWindow.xaml.cs`.
- **DockPanel LastChildFill:** Don't use DockPanel to wrap the file list + Popup — the Popup steals fill space. Use a Grid with `Auto`/`*` rows instead.
- **`{x:Null}` keybindings:** WPF KeyBinding requires a non-null Command. Never use `Command="{x:Null}"` — it silently breaks the window.
- **DataType on keyed DataTemplates:** Don't add `DataType="{x:Type ...}"` to keyed DataTemplates in App.xaml — it can interfere with implicit template resolution.

## Notes
- No new NuGet packages unless explicitly approved.
- Keep all file paths sanitized through `FileSystemService`.
- Root markdown files (`CLAUDE.md`, `CONTINUE.md`, `IMPLEMENTATION_PLAN.md`) should be updated when status changes.

---
