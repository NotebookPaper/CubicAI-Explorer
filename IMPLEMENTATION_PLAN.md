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

### Broader preview support (Markdown and Syntax Highlighting)

- Added rich text preview support using `FlowDocument` and `RichTextBox` for Markdown and Code.
- Implemented a dependency-free Markdown renderer for bold, headers, and lists.
- Implemented regex-based syntax highlighting for C#, XML, JSON, and Python.
- Enhanced `UpdatePreview` to detect and route to rich previews for relevant extensions.

### Bookmark drag/drop feedback and visual cues

- added inline bookmark drag hint text so the sidebar explains folder, sibling, root, and invalid drops in plain language
- highlighted active bookmark drop targets and the root bookmark surface during drag operations
- centralized bookmark drop validation and added smoke coverage for drag-feedback state transitions

### New-file templates support

- added a configurable new-file template folder to preferences and persisted it in settings
- the Edit menu and background file-list context menus now populate templates dynamically from disk
- template-based file creation preserves contents and participates in the existing undo/redo history flow
- added smoke coverage for template catalog loading, settings persistence, and template-based create/undo/redo

### Recycle bin management from the app

- added a Tools-menu `Empty Recycle Bin...` action with explicit confirmation in the window layer
- routed recycle-bin emptying through `IFileSystemService` and `SHEmptyRecycleBinW` instead of direct filesystem deletion
- added smoke coverage for the command path and success status messaging

### Shell verb execution for alternate launches

- added `IFileSystemService.ExecuteShellVerb` so shell verbs stay routed through the filesystem abstraction
- added `Open in New Window` for folders/current pane paths via the Windows `opennewwindow` shell verb
- added `Run as Administrator` for a single selected item via the Windows `runas` shell verb
- added smoke coverage that records the requested verb and path without invoking real shell UI

### File watcher hardening for synced settings and bookmarks

- replaced the narrow last-write watcher callbacks with debounced multi-event watchers for settings and bookmarks
- added recovery for delete/recreate, rename/replace, and watcher-error scenarios so cross-instance sync survives common external save patterns
- added smoke coverage for external settings recreation and bookmark replacement flows

### App icon refresh (v2)

- updated the project output icon metadata to use `Resources\appicon-v2.ico`
- updated the main window icon binding to use the same v2 ICO resource at runtime
- added smoke coverage to verify icon wiring and the presence of 16/32/48/256 icon frames

### Shell property exposure (IPropertyStore) for details and properties

- Implemented `ShellPropertyHelper` using `SHGetPropertyStoreFromParsingName` and `IPropertyStore` for robust metadata retrieval.
- Added "Company", "Version", "Dimensions", and "Duration" columns to the Details view.
- Updated `MainWindow` and `MainViewModel` to handle these new columns (toggling, sorting, persistence).
- Enhanced the internal Properties dialog to display these shell properties in a dedicated details section.
- Updated smoke tests to verify property retrieval and account for new default column set.

### Windows Shell context menu integration (including background menu)

- Added `UseShellContextMenu` property to `UserSettings` with a preference toggle in the UI.
- Implemented `ShellContextMenuHelper` with support for `IContextMenu`, `IContextMenu2`, and `IContextMenu3` correctly via window subclassing.
- Implemented "background" shell context menu when right-clicking empty space in the file list.

### Unified Reveal and Native Properties

- Unified `RevealInExplorer` to use the native `SHOpenFolderAndSelectItems` API.
- Added `ShowNativeProperties` integration using `ShellExecuteEx` for the official Windows properties dialog.

### Shell-aware display names and known-folder aliases

- added shell-backed display-name handling for tab titles, breadcrumbs, recent folders, and new bookmark labels
- the address bar now resolves well-known folder aliases such as `Desktop`, `Documents`, `Downloads`, `Pictures`, `Music`, `Videos`, and `Home`
- address autocomplete now suggests matching known folders alongside filesystem path completion

### Shell-backed type metadata across details, preview, and properties

- centralized shell file-info lookups so Windows-reported type names now replace extension-derived fallbacks where available
- bookmark properties now populate real timestamps, attributes, size, and shell type labels

### Named sessions and session manager

- added persisted named sessions inside the existing settings file
- added session save-as, update, load, delete, and startup-session selection from the main window File menu

### Richer filter and search model

- added explicit `Contains`, `Wildcard`, and `Exact` match modes for inline filter and recursive search workflows
- saved searches now persist their selected match mode and replay with the same semantics

### View-style and column customization

- added persisted details-column layout settings for width, visibility, and order
- the View menu now exposes column toggles, explicit move-left/move-right commands, auto-size, and reset actions

### Tab-management parity: close-left and close-right

- added tab context-menu actions to close all tabs to the left or right of the chosen tab
- smoke coverage now verifies close-left and close-right behavior alongside duplicate and close-others

### Tab reuse for bookmark-driven navigation

- bookmark `Open in New Tab` now reuses an already-open tab for the same folder instead of silently creating duplicates
- bookmark categories opened through `Open All in Tabs` inherit the same reuse behavior

### Crowded-tab overflow affordances

- the main tab strip now supports horizontal scrolling when the header row overflows
- added explicit left/right tab-strip scroll buttons plus a `More Tabs` dropdown listing every open tab

### Edit menu and advanced operations

- added full original CubicExplorer Edit menu parity
- commands: `Duplicate` (Ctrl+D), `Copy Path` (Ctrl+R), `New File`, `Create Symbolic Link`
- `Invert Selection` (Ctrl+Shift+A) for active file list

### Session persistence and machine sync

- added persistent session state: open tabs, active tab, pane paths, and window layout
- implemented non-locking settings/bookmark access for OneDrive compatibility
- added `FileSystemWatcher` support for live auto-refresh of bookmarks and settings

### Transfer reliability and safety

- conflict-aware paste flow supports `KeepBoth`, `Replace`, and `Skip`
- **Safer Replace:** implemented "stage and rename" logic to prevent data loss if a replace operation fails mid-transfer

### Bookmark management and sync hardening

- refactored bookmark management into a dedicated `BookmarkService`
- implemented reliable sync with automatic retries and hardened `FileSystemWatcher` callbacks

### Expanded smoke-test coverage

- added verification for same-folder `Duplicate` behavior and its undo/redo path
- added coverage for failed `Replace` transfers, `New File`, and `Create Symbolic Link` operations

## Verification

Verified on the current branch:

1. `dotnet build CubicAIExplorer.sln -v minimal`
2. `dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal`
3. `tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe`

Smoke coverage explicitly includes:

- shell property retrieval (IPropertyStore)
- transfer conflicts and keep-both behavior
- queued file operation behavior and status text
- ZIP listing and extraction
- archive preview metadata
- hierarchical bookmark import and persistence
- cross-session tab restoration

## Next Planned Work

### 1. UX polish and advanced operations
Status: COMPLETE
Scope:
- completed: new-file templates support (parity with original CubicExplorer)
- completed: app icon refresh with the v2 multi-size icon asset

### 2. Deeper shell integration (continued)
Status: COMPLETE
Scope:
- completed: recycle bin management (empty recycle bin from app)
- completed: shell execution with different verbs (`opennewwindow`, `runas`)

### 3. Infrastructure and reliability
Status: PLANNED
Scope:
- improve error reporting in the file operation queue history

## Constraints And Decisions

- stay on `.NET 8` / WPF with no new NuGet packages unless explicitly approved
- keep all file paths sanitized through `FileSystemService`
- do not delete existing targets before replacement copy/move succeeds (safety first)
- keep settings and bookmark watchers active for multi-instance synchronization

## Known Gotchas

- WPF markup generation can intermittently lock `App.g.cs` or `CubicAIExplorer_MarkupCompile.cache`; rerun the build when it happens
- `IMPLEMENTATION_PLAN.md` and `CONTINUE.md` should be kept in sync when roadmap state changes materially
