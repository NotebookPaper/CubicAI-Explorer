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

### Shell-aware display names and known-folder aliases

- added shell-backed display-name handling for tab titles, breadcrumbs, recent folders, and new bookmark labels so Windows-known folders no longer fall back to raw path segments
- the address bar now resolves well-known folder aliases such as `Desktop`, `Documents`, `Downloads`, `Pictures`, `Music`, `Videos`, and `Home`
- address autocomplete now suggests matching known folders alongside filesystem path completion
- smoke coverage now verifies alias navigation, shell display-name routing, and known-folder autocomplete suggestions

Primary files:

- `src/CubicAIExplorer/Services/FileSystemService.cs`
- `src/CubicAIExplorer/Services/IFileSystemService.cs`
- `src/CubicAIExplorer/ViewModels/MainViewModel.cs`
- `src/CubicAIExplorer/ViewModels/TabViewModel.cs`
- `src/CubicAIExplorer/App.xaml.cs`
- `tests/CubicAIExplorer.SmokeTests/Program.cs`

### Shell-backed type metadata across details, preview, and properties

- centralized shell file-info lookups so Windows-reported type names now replace extension-derived fallbacks where available
- the Details `Type` column, preview header, recursive search results, and properties dialog now stay aligned on the same shell-backed metadata
- bookmark properties now populate real timestamps, attributes, size, and shell type labels instead of placeholder defaults
- smoke coverage now verifies shell-backed type descriptions in directory listings, recursive search results, and the properties dialog

Primary files:

- `src/CubicAIExplorer/Services/ShellFileInfoHelper.cs`
- `src/CubicAIExplorer/Models/FileSystemItem.cs`
- `src/CubicAIExplorer/Services/FileSystemService.cs`
- `src/CubicAIExplorer/ViewModels/FileListViewModel.cs`
- `src/CubicAIExplorer/ViewModels/MainViewModel.cs`
- `tests/CubicAIExplorer.SmokeTests/Program.cs`

### Explorer reveal behavior for selected items

- `Open in Explorer` now reveals a single selected file or folder with Explorer's selection affordance instead of always opening the current folder generically
- when there is no selection or multiple selected items, the command falls back to opening the current folder so the workflow stays predictable
- shell-launch behavior is now routed through `FileSystemService`, keeping sanitization and interop logic out of the window code-behind
- smoke coverage now verifies the command targets the selected item path

Primary files:

- `src/CubicAIExplorer/Services/IFileSystemService.cs`
- `src/CubicAIExplorer/Services/FileSystemService.cs`
- `src/CubicAIExplorer/ViewModels/MainViewModel.cs`
- `src/CubicAIExplorer/MainWindow.xaml.cs`
- `tests/CubicAIExplorer.SmokeTests/Program.cs`

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

### View-style and column customization

- added persisted details-column layout settings for width, visibility, and order inside the existing settings file
- the View menu now exposes column toggles, explicit move-left/move-right commands, auto-size, and reset actions
- both panes recreate the details view from the persisted layout so the same configuration survives restart and mode switches
- smoke coverage now verifies default column layout behavior, normalized saves, and settings round-trip persistence

### Tab-management parity: close-left and close-right

- added tab context-menu actions to close all tabs to the left or right of the chosen tab
- tab cleanup now preserves event-detach behavior through a shared close helper and promotes the clicked tab when the active tab was closed
- smoke coverage now verifies close-left and close-right behavior alongside duplicate and close-others

Primary files:

- `src/CubicAIExplorer/Models/DetailsColumnId.cs`
- `src/CubicAIExplorer/Models/DetailsColumnSetting.cs`
- `src/CubicAIExplorer/Models/UserSettings.cs`
- `src/CubicAIExplorer/ViewModels/MainViewModel.cs`
- `src/CubicAIExplorer/MainWindow.xaml`
- `src/CubicAIExplorer/MainWindow.xaml.cs`
- `tests/CubicAIExplorer.SmokeTests/Program.cs`

### Tab reuse for bookmark-driven navigation

- bookmark `Open in New Tab` now reuses an already-open tab for the same folder instead of silently creating duplicates
- bookmark categories opened through `Open All in Tabs` inherit the same reuse behavior, so only unopened folders create new tabs
- smoke coverage now verifies single-bookmark and category-based reuse paths

Primary files:

- `src/CubicAIExplorer/ViewModels/MainViewModel.cs`
- `tests/CubicAIExplorer.SmokeTests/Program.cs`

### Crowded-tab overflow affordances

- the main tab strip now supports horizontal scrolling when the header row overflows instead of compressing tabs into unusable widths
- added explicit left/right tab-strip scroll buttons plus a `More Tabs` dropdown listing every open tab with the active tab marked
- when tab count or window width changes, the active tab is automatically scrolled back into view
- smoke coverage now verifies the overflow wiring is present in the main window

Primary files:

- `src/CubicAIExplorer/MainWindow.xaml`
- `src/CubicAIExplorer/MainWindow.xaml.cs`
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

### 1. Deeper shell integration

Why this matters:

- CubicExplorer's identity was tightly tied to Windows shell behavior, metadata, and special-folder handling
- the rewrite already has shell icons and basic properties, but shell-native detail still has room to grow

Scope:

- expose more shell metadata in details/properties views where practical
- review shell context behavior and remaining Explorer interop edge cases

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
