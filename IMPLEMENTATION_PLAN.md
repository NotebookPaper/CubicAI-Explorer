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

### Named sessions and session manager

- added persisted named sessions inside the existing settings file
- added session save-as, update, load, delete, and startup-session selection from the main window File menu
- startup now supports either generic last-state restore or a chosen named session
- smoke coverage now verifies save/load/delete/startup session behavior

Primary files:

- `src/CubicAIExplorer/Models/NamedSession.cs`
- `src/CubicAIExplorer/Models/UserSettings.cs`
- `src/CubicAIExplorer/ViewModels/MainViewModel.cs`
- `src/CubicAIExplorer/MainWindow.xaml`
- `src/CubicAIExplorer/MainWindow.xaml.cs`
- `tests/CubicAIExplorer.SmokeTests/Program.cs`

### Richer filter and search model

- added explicit `Contains`, `Wildcard`, and `Exact` match modes for inline filter and recursive search workflows
- saved searches now persist their selected match mode and replay with the same semantics
- filter history is reusable from the main window and persisted in existing settings
- users can opt into clearing inline filters automatically when navigating to a different folder

Primary files:

- `src/CubicAIExplorer/Models/NameMatchMode.cs`
- `src/CubicAIExplorer/Models/UserSettings.cs`
- `src/CubicAIExplorer/Models/SavedSearchItem.cs`
- `src/CubicAIExplorer/ViewModels/FileListViewModel.cs`
- `src/CubicAIExplorer/ViewModels/MainViewModel.cs`
- `src/CubicAIExplorer/MainWindow.xaml`
- `tests/CubicAIExplorer.SmokeTests/Program.cs`

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

The rewrite is already past "basic file manager" parity. The remaining gap with original CubicExplorer is mostly in the power-user shell/workspace layer rather than core file operations.

### 1. View-style and column customization

Why this matters:

- this is one of the clearest areas where the rewrite still feels simpler than the original
- the original offered deeper control over columns, sorting visibility, and view behavior than the current fixed `GridView` setup

Scope:

- persist per-view or global column widths/order/visibility
- support richer shell/detail columns where practical
- add optional auto-size / always-show-sort-column behavior
- evaluate whether grouping / arrange-by is worth implementing before thumbs-style work

Likely files:

- `src/CubicAIExplorer/MainWindow.xaml.cs`
- `src/CubicAIExplorer/ViewModels/FileListViewModel.cs`
- `src/CubicAIExplorer/Models/UserSettings.cs`
- `src/CubicAIExplorer/Services/FileSystemService.cs`

### 2. Tab-management parity

Why this matters:

- the rewrite already has duplicate tab and close others, so the missing pieces are incremental and high-value
- original CubicExplorer exposed more tab/session management power, including close-left / close-right and related affordances

Scope:

- add close tabs on left / right
- consider "reuse already open tabs" navigation behavior
- evaluate tab overflow / more-tabs affordances once command parity is in place

Likely files:

- `src/CubicAIExplorer/ViewModels/MainViewModel.cs`
- `src/CubicAIExplorer/MainWindow.xaml`
- `src/CubicAIExplorer/MainWindow.xaml.cs`
- `tests/CubicAIExplorer.SmokeTests/Program.cs`

### 3. Deeper shell integration

Why this matters:

- CubicExplorer's identity was tightly tied to Windows shell behavior, metadata, and special-folder handling
- the rewrite already has shell icons and basic properties, but shell-native detail still has room to grow

Scope:

- improve special-folder coverage and path/display handling
- expose more shell metadata in details/properties views where practical
- review shell context behavior and Explorer interop edge cases

Likely files:

- `src/CubicAIExplorer/Services/FileSystemService.cs`
- `src/CubicAIExplorer/Services/ShellIconService.cs`
- `src/CubicAIExplorer/Models/FileSystemItem.cs`
- `src/CubicAIExplorer/MainWindow.xaml.cs`

### Lower-priority follow-up after parity-critical work

- archive UX beyond browse/filter/extract
- broader preview type support
- richer queue history and per-item result reporting
- UX polish on bookmark drag/drop, symbolic link feedback, and new-file templates

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
