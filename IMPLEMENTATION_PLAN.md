# CubicAI Explorer — Implementation Plan

> **Updated:** 2026-03-11
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

### Windows Shell context menu integration (including background menu)

- Added `UseShellContextMenu` property to `UserSettings` with a preference toggle in the UI.
- Implemented `ShellContextMenuHelper` with support for `IContextMenu`, `IContextMenu2`, and `IContextMenu3` to handle submenus (e.g., "Send To", "Open With") correctly via window subclassing.
- Added shell context menu support to `FileListView`, `FolderTree`, and `BookmarkTree` (for filesystem bookmarks).
- Implemented "background" shell context menu when right-clicking empty space in the file list, providing "New", "Paste", and "Refresh" parity with Explorer.
- Updated `MainWindow` to intercept context menu events and show the native menu when enabled.

Primary files:

- `src/CubicAIExplorer/Services/ShellContextMenuHelper.cs`
- `src/CubicAIExplorer/MainWindow.xaml.cs`
- `src/CubicAIExplorer/Models/UserSettings.cs`

### Unified Reveal and Native Properties

- Unified `RevealInExplorer` to use the native `SHOpenFolderAndSelectItems` API for both single and multiple selections, providing a more consistent and robust experience than calling `explorer.exe /select`.
- Added `ShowNativeProperties` integration using `ShellExecuteEx` with `SEE_MASK_INVOKEIDLIST` to host the official Windows properties dialog.
- Updated `MainWindow` and `MainViewModel` to prefer the native properties dialog when shell integration is enabled, covering file list items and bookmarks.
- Modernized `OpenInDefaultApp` to use `ShellExecute` directly on paths for standard folder and file launches.

### Shell-aware display names and known-folder aliases

- added shell-backed display-name handling for tab titles, breadcrumbs, recent folders, and new bookmark labels so Windows-known folders no longer fall back to raw path segments
- the address bar now resolves well-known folder aliases such as `Desktop`, `Documents`, `Downloads`, `Pictures`, `Music`, `Videos`, and `Home`
- address autocomplete now suggests matching known folders alongside filesystem path completion
- smoke coverage now verifies alias navigation, shell display-name routing, and known-folder autocomplete suggestions

### Shell-backed type metadata across details, preview, and properties

- centralized shell file-info lookups so Windows-reported type names now replace extension-derived fallbacks where available
- the Details `Type` column, preview header, recursive search results, and properties dialog now stay aligned on the same shell-backed metadata
- bookmark properties now populate real timestamps, attributes, size, and shell type labels instead of placeholder defaults
- smoke coverage now verifies shell-backed type descriptions in directory listings, recursive search results, and the properties dialog

### Named sessions and session manager

- added persisted named sessions inside the existing settings file
- added session save-as, update, load, delete, and startup-session selection from the main window File menu
- startup now supports either generic last-state restore or a chosen named session
- smoke coverage now verifies save/load/delete/startup session behavior

### Richer filter and search model

- added explicit `Contains`, `Wildcard`, and `Exact` match modes for inline filter and recursive search workflows
- saved searches now persist their selected match mode and replay with the same semantics
- filter history is reusable from the main window and persisted in existing settings
- users can opt into clearing inline filters automatically when navigating to a different folder

### View-style and column customization

- added persisted details-column layout settings for width, visibility, and order inside the existing settings file
- the View menu now exposes column toggles, explicit move-left/move-right commands, auto-size, and reset actions
- both panes recreate the details view from the persisted layout so the same configuration survives restart and mode switches
- smoke coverage now verifies default column layout behavior, normalized saves, and settings round-trip persistence

### Tab-management parity: close-left and close-right

- added tab context-menu actions to close all tabs to the left or right of the chosen tab
- tab cleanup now preserves event-detach behavior through a shared close helper and promotes the clicked tab when the active tab was closed
- smoke coverage now verifies close-left and close-right behavior alongside duplicate and close-others

### Tab reuse for bookmark-driven navigation

- bookmark `Open in New Tab` now reuses an already-open tab for the same folder instead of silently creating duplicates
- bookmark categories opened through `Open All in Tabs` inherit the same reuse behavior, so only unopened folders create new tabs
- smoke coverage now verifies single-bookmark and category-based reuse paths

### Crowded-tab overflow affordances

- the main tab strip now supports horizontal scrolling when the header row overflows instead of compressing tabs into unusable widths
- added explicit left/right tab-strip scroll buttons plus a `More Tabs` dropdown listing every open tab with the active tab marked
- when tab count or window width changes, the active tab is automatically scrolled back into view
- smoke coverage now verifies the overflow wiring is present in the main window

### Edit menu and advanced operations

- added full original CubicExplorer Edit menu parity
- commands: `Duplicate` (Ctrl+D), `Copy Path` (Ctrl+R), `New File`, `Create Symbolic Link`
- `Invert Selection` (Ctrl+Shift+A) for active file list
- improved toolbar with inline Address/Breadcrumb bar and "New Tab" button
- hierarchical bookmark organization with folders and drag-and-drop

### Session persistence and machine sync

- added persistent session state: open tabs, active tab, pane paths, and window layout (sidebar/preview widths)
- implemented non-locking settings/bookmark access for OneDrive compatibility
- added `FileSystemWatcher` support for live auto-refresh of bookmarks and settings when updated externally
- environment variable overrides (`CUBICAI_SETTINGS_PATH`, `CUBICAI_BOOKMARKS_PATH`) for flexible sync location

### Transfer reliability and safety

- conflict-aware paste flow supports `KeepBoth`, `Replace`, and `Skip`
- **Safer Replace:** implemented "stage and rename" logic to prevent data loss if a replace operation fails mid-transfer
- transfer history keeps partial results instead of collapsing mixed outcomes
- clipboard handling is more robust against transient failures and Explorer drop-effect variants

### Bookmark management and sync hardening

- refactored bookmark management into a dedicated `BookmarkService` to share robust persistence logic with `SettingsService`
- implemented reliable sync with automatic retries and hardened `FileSystemWatcher` callbacks to handle transient file locks
- organized bookmark data access into an atomic load/save pattern with watcher suspension during local writes

### Expanded smoke-test coverage

- added verification for same-folder `Duplicate` behavior and its undo/redo path
- added coverage for risky file-operation paths, specifically verifying backup restoration after failed `Replace` transfers
- verified undo/redo for `New File` and `Create Symbolic Link` operations
- hardened the smoke harness to support multi-window tests without process shutdown races

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
Status: IN_PROGRESS
Scope:
- implement "background" shell context menu when right-clicking empty space in the file list (COMPLETE)
- review remaining Explorer interop edge cases beyond reveal/select behavior
- explore shell metadata exposure in details/properties views

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
