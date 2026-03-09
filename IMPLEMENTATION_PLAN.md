# CubicAI Explorer — Implementation Plan

> **Updated:** 2026-03-09  
> **Tier 1 status:** Complete

## Context
Tier 1 was fully implemented and then extended. This document now tracks:
1. Completed Tier 1 scope (historical record)
2. Completed post-Tier-1 improvements
3. Next planned slices

## Tier 1 Implementation (Completed)

### Phase 1 — Foundation Services (done)

**1a. File Operations** — extend `IFileSystemService` + `FileSystemService`
- Add: `CopyFiles`, `MoveFiles`, `DeleteFiles`, `RenameFile`, `CreateFolder`
- Delete uses Recycle Bin by default via `Microsoft.VisualBasic.FileIO.FileSystem` (no extra NuGet needed)
- All paths go through existing `SanitizePath()`
- Name collisions handled with numeric suffix ("file (2).txt")
- Files: `Services/IFileSystemService.cs`, `Services/FileSystemService.cs`

**1b. Clipboard Service** — new `IClipboardService` + `ClipboardService`
- Uses Windows clipboard with `CF_HDROP` + `Preferred DropEffect` format
- Full interop: copy in CubicAI → paste in Windows Explorer (and vice versa)
- `SetFiles(paths, isCut)`, `GetFiles()`, `HasFiles()`
- New files: `Services/IClipboardService.cs`, `Services/ClipboardService.cs`

**1c. Shell Icon Service** — new `IShellIconService` + `ShellIconService`
- P/Invoke `SHGetFileInfo` from `shell32.dll`
- Extension-based cache for files, path-based for folders/drives
- `BitmapSource.Freeze()` for thread safety
- New files: `Services/IShellIconService.cs`, `Services/ShellIconService.cs`

### Phase 2 — Commands & Dialogs (done)

**2a. Simple Dialogs** — for rename and new folder
- `RenameDialog` — TextBox pre-filled with current name, OK/Cancel
- `NewFolderDialog` — TextBox defaulting to "New folder", OK/Cancel
- New files: `Views/RenameDialog.xaml(.cs)`, `Views/NewFolderDialog.xaml(.cs)`

**2b. FileListViewModel Commands** — the core feature addition
- New commands: `Copy`, `Cut`, `Paste`, `Delete`, `PermanentDelete`, `Rename`, `NewFolder`, `Refresh`, `SelectAll`
- Add `ObservableCollection<FileSystemItem> SelectedItems` for multi-select
- Add `IClipboardService` constructor dependency
- Delete shows confirmation MessageBox
- After each operation, calls `Refresh()` to reload file list
- Wire `IClipboardService` through `MainViewModel` → `TabViewModel` → `FileListViewModel`
- Update: `ViewModels/FileListViewModel.cs`, `ViewModels/TabViewModel.cs`, `ViewModels/MainViewModel.cs`, `App.xaml.cs`

### Phase 3 — View Integration (done)

**3a. Context Menu** — right-click on file list
- On items: Open, Cut, Copy, Paste, Delete, Rename
- On empty space: Paste, New Folder, Refresh
- Toggle visibility via `ContextMenuOpening` code-behind handler
- Update: `MainWindow.xaml`, `MainWindow.xaml.cs`

**3b. Keyboard Shortcuts** — `InputBindings` on Window
| Key | Action |
|-----|--------|
| Ctrl+C | Copy |
| Ctrl+X | Cut |
| Ctrl+V | Paste |
| Delete | Delete (Recycle Bin) |
| Shift+Delete | Permanent Delete |
| F2 | Rename |
| F5 | Refresh |
| Ctrl+Shift+N | New Folder |
| Ctrl+A | Select All |
- Update: `MainWindow.xaml`

**3c. Shell Icons** — replace emoji placeholders
- New `ShellIconConverter` replaces `FileIconConverter`
- Change icon `TextBlock` to `Image` control in file list and folder tree
- Set `ShellIconConverter.IconService` static property from `App.xaml.cs`
- New file: `Converters/ShellIconConverter.cs`
- Update: `MainWindow.xaml`, `App.xaml`, `App.xaml.cs`
- Delete: `Converters/FileIconConverter.cs`

**3d. Multi-Select** — `SelectionMode="Extended"` on ListView
- Sync `ListView.SelectedItems` → `FileListViewModel.SelectedItems` via `SelectionChanged` code-behind
- Handle `SelectAllRequested` event from ViewModel to call `ListView.SelectAll()`
- Update: `MainWindow.xaml`, `MainWindow.xaml.cs`

## Tier 1 File Summary

### New files (9):
| File | Purpose |
|------|---------|
| `Services/IClipboardService.cs` | Clipboard interface |
| `Services/ClipboardService.cs` | Windows clipboard with Explorer interop |
| `Services/IShellIconService.cs` | Shell icon interface |
| `Services/ShellIconService.cs` | SHGetFileInfo P/Invoke + cache |
| `Converters/ShellIconConverter.cs` | IValueConverter using ShellIconService |
| `Views/RenameDialog.xaml` + `.cs` | Rename dialog |
| `Views/NewFolderDialog.xaml` + `.cs` | New folder dialog |

### Modified files (9):
| File | Changes |
|------|---------|
| `Services/IFileSystemService.cs` | +5 method signatures |
| `Services/FileSystemService.cs` | +5 method implementations |
| `ViewModels/FileListViewModel.cs` | +IClipboardService, +SelectedItems, +9 commands |
| `ViewModels/TabViewModel.cs` | Pass IClipboardService through |
| `ViewModels/MainViewModel.cs` | Accept + pass IClipboardService |
| `MainWindow.xaml` | Context menu, shortcuts, SelectionMode, Image icons |
| `MainWindow.xaml.cs` | SelectionChanged, ContextMenuOpening, SelectAll |
| `App.xaml` | Swap converter resource |
| `App.xaml.cs` | Create + wire new services |

### Deleted files (1):
| File | Reason |
|------|--------|
| `Converters/FileIconConverter.cs` | Replaced by ShellIconConverter |

## Post-Tier-1 Completed Enhancements

1. Inline rename in file list (replaced modal rename workflow)
2. Bookmarks MVP + persistence to `%AppData%\\CubicAIExplorer\\bookmarks.json`
3. Drag/drop copy/move in file list
4. Undo/redo history for rename/copy/move/new-folder/permanent-delete
5. Clear History command
6. Same-folder move no-op guard
7. Classic Cubic-inspired theme pass (typography/chrome/pane styling)
8. Smoke-test harness (`tests/CubicAIExplorer.SmokeTests`) with regression coverage
9. Full toolbar with Cut/Copy/Paste/Delete/Undo/Redo/Refresh buttons
10. View mode switching (Details/List/Tiles) with data templates
11. Selection count in status bar
12. Expanded smoke tests (19 total: redo copy, redo move, view mode, selection status)
13. Search/filter bar (Ctrl+F) with real-time name filtering
14. File properties dialog (size, type, dates, attributes, folder contents count)
15. Drag/drop files to folder tree nodes
16. Sort indicator arrows (▲/▼) on column headers
17. Tab context menu (Duplicate, Close, Close Other Tabs)
18. Status bar total size of selected files
19. Open in Explorer from context menu
20. Enter key opens selected item
21. Window size/position persistence (`%AppData%\CubicAIExplorer\window.json`)
22. Expanded smoke tests (24 total: filter, properties, duplicate tab, close others, size status)
23. Breadcrumb-style address bar
24. Recent folders panel
25. Recursive search across subdirectories
26. Toolbar vector icon assets replacing Unicode glyphs
27. Address bar autocomplete suggestions
28. Dual-pane mode
29. Preview panel for text and common image files
30. Active-pane routing for commands and navigation in dual-pane mode
31. Right-pane context menu, sort, rename, select-all, and properties parity
32. Active-pane filter/search/view-mode routing
33. Active-pane highlighting and per-pane status presentation
34. Current-pane navigation from breadcrumbs, recent folders, bookmarks, tree selection, and autocomplete
35. Preview empty/error/size-limit states
36. Right-pane inline address editing from the pane header
37. Expanded smoke tests covering dual-pane routing, preview refresh, preview states, and address suggestions

## Next Planned Features

1. **Preview polish**
- Expand supported previewable formats
- Move preview loading off the UI thread where practical
- Improve image/text fallback behavior further

2. **Address autocomplete polish**
- Improve root and partial-drive completion
- Tighten keyboard navigation and selection behavior

3. **Visual polish**
- Refine pane spacing, borders, and splitter behavior to better match CubicExplorer

4. **Address workflow consistency**
- Decide whether the left-pane/shared top bar should gain a more explicit inline-edit affordance to better match the right-pane header editor

## NuGet Packages
None added. All APIs are in .NET 8 + WPF SDK.

## Verification (Current)
1. `dotnet build` — zero errors, zero warnings
2. Run app → right-click file → Copy → navigate to another folder → Paste → file appears
3. Copy file in app → paste in Windows Explorer → file appears (clipboard interop)
4. Copy file in Windows Explorer → paste in app → file appears
5. Select file → Delete → confirm → file goes to Recycle Bin
6. F2 → inline rename editor appears in list → Enter commits, Esc cancels
7. Ctrl+Shift+N → new folder dialog → folder created
8. All files show real Windows icons, not emoji
9. Multi-select with Ctrl+Click and Shift+Click works
10. Smoke tests pass:
   - `dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal`
   - `tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe`

## Key Design Decisions
- **Recycle Bin delete** via `Microsoft.VisualBasic.FileIO.FileSystem` — simplest reliable approach, no P/Invoke needed
- **Clipboard interop** uses `CF_HDROP` + `Preferred DropEffect` DWORD — the exact format Windows Explorer uses
- **Shell icons** cached by extension (files) or path (folders/drives) — avoids per-file disk access
- **Multi-select** synced via code-behind `SelectionChanged` handler — WPF's `SelectedItems` isn't bindable
- **All file paths** go through `SanitizePath()` (calls `Path.GetFullPath()`) before any operation
- **No new NuGet packages** — everything is in .NET 8 SDK + WPF
