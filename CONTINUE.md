# Continuation Instructions for Next Session

> **Last updated:** 2026-03-10
> **Status:** Tier 1 complete + dual-pane, preview, autocomplete, preferences, active-pane proxy cleanup, preview metadata expansion, keyboard accessibility polish, and startup crash fixes are implemented and pushed. Smoke suite passes.

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
- Preview expanded with PDF metadata and media duration/dimensions metadata fallback
- Address bar autocomplete with drive-root completion and debounced async suggestions
- Keyboard accessibility shortcuts and focus polish (`Ctrl+1..4`, `Alt+D`, `Ctrl+Shift+L`, list keyboard handlers, explicit tab order)

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

**Committed on `origin/master`:**
- `8ad16c9` — async preview, drive-root autocomplete, debounced suggestions, right-pane autocomplete
- `6445720` — preferences/settings persistence + smoke coverage
- `2cd2788` — active-pane UI now binds directly to `CurrentPaneFileList.*`; removed `Current*` forwarding proxies and related wiring
- `b5e59e9` — expanded preview metadata (PDF/media) + keyboard navigation shortcuts/focus polish
- `e2b513f` — fixed startup crashes (read-only binding mode + suggestion UI-thread safety) + smoke guards
- `70a4a1a` — refactored pane command forwarding through shared helpers in `MainViewModel`

**Worktree:** clean for tracked files (only local untracked utility paths below).

Untracked local-only paths:
- `.claude/`
- `src/CubicAIExplorer/obj_verify/`
- `tests/CubicAIExplorer.SmokeTests/obj_verify/`

## Verification
Verified on **2026-03-10**:
- `dotnet build CubicAIExplorer.sln` — 0 errors, 0 warnings
- Smoke tests: 42/42 pass (includes settings coverage, preview metadata coverage, and startup-regression guards)

## Priority Next Work
1. **Manual UX verification pass** for new keyboard shortcuts and media metadata behavior on real files.
2. **Preview roadmap (optional):** improve PDF page-count accuracy and richer audio/video metadata if future package approvals are allowed.
3. **Optional architecture cleanup:** evaluate splitting `MainViewModel` into smaller focused collaborators if command surface keeps growing.

## Known Gotchas
- **WPF markup build lock:** smoke-test project builds can fail in the sandbox with `App.g.cs` / `MarkupCompile.cache` access-denied errors under `src\CubicAIExplorer\obj\Debug\net8.0-windows`. Re-running the smoke-test build outside the sandbox resolves it.
- **View mode bug (fixed):** `ApplyViewMode("Details")` must not run during tab initialization, or it replaces the XAML-defined `GridView` and breaks rendering.
- **DockPanel LastChildFill:** wrapping a file list and popup in a `DockPanel` causes layout issues. Use a `Grid` with explicit rows.
- **KeyBinding null commands:** WPF `KeyBinding` requires a non-null command.
- **Keyed DataTemplate + DataType:** avoid combining them in `App.xaml`.
- **Right-pane header double-click:** `Border` does not support a `MouseDoubleClick` XAML event. Handle double-click via `MouseLeftButtonDown` and `ClickCount`.
- **Forwarding commands (partially reduced):** command forwarding now uses shared helper methods, but command surface remains large due to top-level keybindings and toolbar wiring.
- **WinForms namespace collisions:** Adding `<UseWindowsForms>true</UseWindowsForms>` to the csproj causes widespread `Point`, `Brush`, `DragEventArgs`, `ListView` ambiguities. Use `Microsoft.Win32.OpenFolderDialog` (.NET 8+) instead of `System.Windows.Forms.FolderBrowserDialog`.

## Notes
- No new NuGet packages unless explicitly approved.
- Keep all file paths sanitized through `FileSystemService`.
- Root markdown files (`CLAUDE.md`, `CONTINUE.md`, `IMPLEMENTATION_PLAN.md`) should stay in sync with the actual repo state.

---
