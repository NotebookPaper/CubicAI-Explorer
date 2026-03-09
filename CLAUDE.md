# CubicAI Explorer

> This file is the single source of truth for AI assistants working on this project.
> Both `CLAUDE.md` (Claude Code) and `AGENTS.md` (OpenAI Codex) point here.
> **If you update project instructions, update this file.**

## Project Overview

- **Name:** CubicAI Explorer — a modern Windows file manager
- C#/WPF rewrite of CubicExplorer (abandoned Delphi file manager)
- The "AI" in the name is a nod to AI-assisted development, not AI features in the app
- **License:** MPL 1.1 (inherited from original)

## Build & Run

```bash
# Build (from repo root)
dotnet build CubicAIExplorer.sln

# Run
dotnet run --project src/CubicAIExplorer/CubicAIExplorer.csproj

# Build release
dotnet publish src/CubicAIExplorer/CubicAIExplorer.csproj -c Release
```

There are no tests yet. When tests are added, they will use `dotnet test`.

Smoke tests are available and can be run with:

```bash
dotnet run --project tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj
# or
./scripts/run-tier1-smoke-tests.ps1
```

## Tech Stack

- **.NET 8 LTS** (not .NET 9) — `net8.0-windows`
- **WPF** with XAML
- **CommunityToolkit.Mvvm** 8.2.2 for MVVM (ObservableObject, RelayCommand, etc.)
- No other NuGet dependencies — keep it minimal
- Windows-only (WPF, shell P/Invoke, named pipes)

## Project Structure

```
CubicAIExplorer.sln
src/CubicAIExplorer/
  App.xaml(.cs)              # Application entry, service wiring
  MainWindow.xaml(.cs)       # Main UI (single window)
  Models/
    FileSystemItem.cs        # File/folder data model
    TabItem.cs               # Browser tab model
    BookmarkItem.cs          # Bookmark model
  Services/
    IFileSystemService.cs    # File system abstraction (interface)
    FileSystemService.cs     # Implementation with path sanitization
    NavigationService.cs     # Back/forward navigation history
    SingleInstanceService.cs # Named-pipe single instance enforcement
  ViewModels/
    MainViewModel.cs         # Top-level VM, owns tabs
    TabViewModel.cs          # Per-tab VM, owns tree + file list
    FileListViewModel.cs     # File listing, sorting, commands
    FolderTreeNodeViewModel.cs # Folder tree node (lazy-loading)
  Converters/
    FileSizeConverter.cs     # Bytes to human-readable
    ShellIconConverter.cs    # Real shell icons via SHGetFileInfo
```

## Architecture & Patterns

### MVVM
- ViewModels inherit from `ObservableObject` (CommunityToolkit.Mvvm)
- Commands use `[RelayCommand]` attribute or `RelayCommand` / `AsyncRelayCommand`
- No code-behind logic except minimal UI wiring (selection sync, dialog hosting)
- Services are injected via constructor (manual DI in App.xaml.cs, no DI container)

### Service Injection Flow
```
App.xaml.cs creates services
  -> MainViewModel(services...)
    -> TabViewModel(services...)
      -> FileListViewModel(services...)
```

### Security
- **Path sanitization:** All file paths go through `SanitizePath()` in `FileSystemService` to prevent path traversal
- **IPC:** Uses named pipes for single-instance communication (not WM_COPYDATA)
- **No shell execution:** Don't use `Process.Start("cmd", ...)` for file operations
- Follow OWASP principles — no command injection, validate all external input

### Conventions
- Use `async/await` for I/O operations
- Use `ObservableCollection<T>` for UI-bound lists
- Prefer `BitmapSource.Freeze()` for any images used cross-thread
- Name collisions in file operations use numeric suffix: `file (2).txt`
- Delete operations use Recycle Bin by default (`Microsoft.VisualBasic.FileIO.FileSystem`)

## Current State

The app has a working UI with:
- Tabbed browsing (create/close tabs)
- Folder tree with lazy-loading
- File list with sorting by name/size/date/type
- Back/forward/up navigation
- Address bar navigation
- Breadcrumb-style address bar
- File operations (copy, move, delete, permanent delete, rename, new folder)
- Clipboard interop with Windows Explorer (`CF_HDROP` + `Preferred DropEffect`)
- Context menu and keyboard shortcuts
- Real shell icons in folder tree and file list
- Multi-select support with selection count in status bar
- Inline rename in file list (F2)
- Drag/drop copy/move in file list
- Bookmarks with persistence (`%AppData%\\CubicAIExplorer\\bookmarks.json`)
- Undo/redo history for rename/copy/move/new-folder/permanent-delete
- Classic Cubic/XP-inspired theme pass
- Full toolbar with Cut/Copy/Paste/Delete/Undo/Redo/Refresh buttons
- View mode switching (Details/List/Tiles) via View menu
- Search/filter bar (Ctrl+F) to filter files by name
- File properties dialog (Alt+Enter or context menu)
- Drag/drop files to folder tree nodes
- Sort indicator arrows on column headers
- Tab context menu (Duplicate, Close, Close Others)
- Status bar shows total size of selected files
- Open in Explorer from context menu
- Enter key opens selected item
- Window size/position persisted across sessions
- Quick access recent folders
- Recursive search within folders
- Address bar autocomplete with drive-root completion and debounced async suggestions
- Dual-pane mode with right-pane autocomplete
- Preview panel with async loading, folder preview, and file metadata fallback

## Reference Material

- Original Delphi source: `cubicexplorer-src/` (read-only reference, do not modify)
- Security analysis of original: see `.claude/projects/` memory files
- Original was Delphi 2006, ~205 .pas files

## Guidelines for AI Assistants

1. **Read before editing** — always read a file before modifying it
2. **Build after changes** — run `dotnet build CubicAIExplorer.sln` to verify
3. **Keep it simple** — no unnecessary abstractions, no over-engineering
4. **No new NuGet packages** unless explicitly discussed and approved
5. **Windows-only is fine** — this is a WPF app, don't add cross-platform abstractions
6. **Don't modify `cubicexplorer-src/`** — it's read-only reference material
7. **Follow existing patterns** — match the style of surrounding code
8. **Security first** — all paths through `SanitizePath()`, no `Process.Start` for file ops
9. **Keep docs current** — if implementation state changes materially, update `CONTINUE.md` and related planning docs
