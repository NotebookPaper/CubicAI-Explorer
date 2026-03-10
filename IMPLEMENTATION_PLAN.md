# CubicAI Explorer — Implementation Plan

> **Updated:** 2026-03-10
> **Status:** Current with `master`

## Context

Tier 1 is long complete. This file now tracks the actual shipped feature set, the recently finished roadmap slices, and the next practical work items.

## Implemented Foundation

The app currently includes:

- tabbed browsing and dual-pane mode
- folder tree with lazy loading
- file list sorting, multi-select, inline rename, drag/drop, and shell icons
- copy, cut, paste, move, delete, permanent delete, create folder, undo/redo, and Explorer clipboard interop
- hierarchical bookmarks with icons, recent folders, breadcrumbs, autocomplete, search/filter, and recursive search
- preview panel with text, image, folder, metadata, and fallback states
- properties dialog, toolbar, context menus, keyboard shortcuts, and persisted window/settings state
- smoke-test harness covering core behaviors and recent regressions
- cross-machine settings and bookmark synchronization via OneDrive or shared paths

## Recently Completed Slices

### Edit menu and advanced operations

- added full original CubicExplorer Edit menu parity
- commands: `Duplicate` (Ctrl+D), `Copy Path` (Ctrl+R), `New File`, `Create Symbolic Link`
- `Invert Selection` (Ctrl+Shift+A) for active file list
- improved toolbar with inline Address/Breadcrumb bar and "New Tab" button
- hierarchical bookmark organization with folders and drag-and-drop

Primary files:

- `src/CubicAIExplorer/ViewModels/FileListViewModel.cs`
- `src/CubicAIExplorer/ViewModels/MainViewModel.cs`
- `src/CubicAIExplorer/MainWindow.xaml`
- `src/CubicAIExplorer/MainWindow.xaml.cs`
- `src/CubicAIExplorer/Services/FileSystemService.cs`

### Session persistence and machine sync

- added persistent session state: open tabs, active tab, pane paths, and window layout (sidebar/preview widths)
- implemented non-locking settings/bookmark access for OneDrive compatibility
- added `FileSystemWatcher` support for live auto-refresh of bookmarks and settings when updated externally
- environment variable overrides (`CUBICAI_SETTINGS_PATH`, `CUBICAI_BOOKMARKS_PATH`) for flexible sync location

Primary files:

- `src/CubicAIExplorer/Models/UserSettings.cs`
- `src/CubicAIExplorer/Services/SettingsService.cs`
- `src/CubicAIExplorer/ViewModels/MainViewModel.cs`
- `src/CubicAIExplorer/App.xaml.cs`

### Transfer reliability and safety

- conflict-aware paste flow supports `KeepBoth`, `Replace`, and `Skip`
- **Safer Replace:** implemented "stage and rename" logic to prevent data loss if a replace operation fails mid-transfer
- transfer history keeps partial results instead of collapsing mixed outcomes
- clipboard handling is more robust against transient failures and Explorer drop-effect variants

Primary files:

- `src/CubicAIExplorer/Services/FileSystemService.cs`
- `src/CubicAIExplorer/Services/IFileSystemService.cs`
- `src/CubicAIExplorer/ViewModels/FileListViewModel.cs`

## Verification

Verified on the current branch:

1. `dotnet build CubicAIExplorer.sln -v minimal`
2. `dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal`
3. `tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe`

Smoke coverage explicitly includes:

- transfer conflicts and keep-both behavior
- clipboard drop-effect parsing
- queued file operation behavior and status text
- queue cancellation and progress reporting
- ZIP listing and extraction
- archive browser filtering
- archive preview metadata
- hierarchical bookmark import and persistence
- cross-session tab restoration
- sidebar width persistence

## Next Planned Work

1. Manual UX pass on the newest slices:
   - verify drag-and-drop bookmark reordering UX
   - check symbolic link creation success feedback
   - ensure "New File" creates usable template-based files if appropriate
2. Archive follow-up:
   - add richer archive actions beyond browse + filter + extract-all
   - decide whether opening an archive should stay metadata-first or become navigable
3. Preview follow-up:
   - extend metadata/preview support for more file types
   - keep expensive preview generation off the UI thread
4. Queue follow-up:
   - preserve richer queue history than the last result only
   - implement per-item failure detail or batch summaries

## Constraints And Decisions

- stay on `.NET 8` / WPF with no new NuGet packages unless explicitly approved
- keep all file paths sanitized through `FileSystemService`
- prefer existing MVVM and service-wiring patterns over new abstractions
- do not delete existing targets before replacement copy/move succeeds (safety first)
- avoid `DockPanel` for file-list plus popup layouts; use explicit `Grid`
- keep settings and bookmark watchers active for multi-instance synchronization

## Known Gotchas

- WPF markup generation can intermittently lock `App.g.cs` or `CubicAIExplorer_MarkupCompile.cache`; rerun the build when it happens
- `IMPLEMENTATION_PLAN.md` and `CONTINUE.md` should be kept in sync when roadmap state changes materially
