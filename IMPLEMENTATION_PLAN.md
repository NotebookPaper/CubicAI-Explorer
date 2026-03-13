# CubicAI Explorer — Implementation Plan

> **Updated:** 2026-03-13
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

### Undo Close Tab

- added a bounded session-local recently-closed-tab stack in `MainViewModel` that captures tab path, title, lock state, locked root, color, and original index
- added `Undo Close Tab` to the File menu, tab context menu, and `Ctrl+Shift+T`, with command enablement tied to stack state
- restore now reopens the most recently closed tab near its original position, reactivates it, preserves lock/color metadata, and avoids tracking session-load or shutdown tab teardown

### Bookmark drop target visibility and precision

- refined bookmark drag targeting to distinguish row-center folder drops (`into`) from sibling-placement drops (`after`)
- added hover-to-expand behavior for collapsed bookmark folders during drag operations so nested folders stay reachable
- updated the bookmark tree visuals and smoke coverage to verify nested child drops, distinct drag hints, and drop-target wiring

### Tab locking and coloring

- added persisted per-tab lock and color metadata while keeping legacy tab-path settings/session payloads backward compatible
- locked tabs now stay bound to their root folder subtree and fork outside navigation into a new tab, including back/forward history hops
- added tab context-menu lock/color actions, colored tab-header accents, and smoke coverage for fork navigation plus reload persistence

### File utilities (split/join/checksum)

- added Tools-menu split, join, and checksum dialogs with active-file prefills for common workflows
- added queue-backed file chunk splitting and contiguous reassembly with numbered suffixes such as `.001`
- added one-pass MD5/SHA1/SHA256 generation plus compare-against-string support in the checksum tool and smoke coverage for the end-to-end behavior

### UI layout manager

- added persisted named window layouts inside the existing settings file
- added a View-menu `Layouts` section with save, manage, and one-click apply actions
- applying a layout now restores sidebar visibility/width, preview visibility/width, dual-pane mode, bookmarks bar visibility, and file-list view mode

### Advanced attribute/date search filters

- added an expandable advanced-search row with attribute, size-range, and modified-date filters alongside the existing name/content search controls
- recursive search now supports combined hidden/system/read-only/archive, size, and inclusive date-range criteria with validation for invalid ranges
- saved searches now persist and replay the advanced criteria, and smoke coverage verifies live filtering plus saved-search round-trips

### Group By and manual sorting

- added a `View > Group By` submenu with name, type, size, and date-modified grouping options for the active pane
- added a `View > Manual Sorting` toggle that switches the pane into drag-reorder mode for folder contents
- persisted per-folder manual sort sequences in the existing settings file and added smoke coverage for date grouping plus reload persistence

### External tools configuration

- added an `External Tools` Preferences tab for persisted tool definitions with name, program path, and optional argument templates such as `%p`
- added a `Tools > External Tools` submenu plus file-list `Open with... (Tools)` context-menu entries populated from the saved tool catalog
- routed tool launches through `IFileSystemService` with selected-file argument expansion and smoke coverage for settings round-trips plus launch behavior

### Drop Stack virtual collection

- added a toggleable `Drop Stack` sidebar pane in the View menu for collecting files and folders across folder changes
- dragging onto the pane now shelves paths without copying or moving anything on disk, and per-entry delete actions only remove the shelf entry
- added `Copy all to...`, `Move all to...`, and `Clear` actions wired through the existing filesystem abstraction and queue service, with smoke coverage for collection and transfer flows

### Code review fixes

- sanitized bookmark/settings/template environment override paths through a shared helper before use
- restricted single-instance named-pipe IPC to the current user and removed viewmodel-owned modal UI through a shared dialog service
- fixed JSON identifier round-trips, archive browse model coupling, async folder/file counting loads, replace-flow TOCTOU edges, clipboard payload ownership, and event-subscription leaks

### Active-pane search race fix

- canceled and invalidated in-flight folder loads before pane-scoped search execution so stale async reloads can no longer mutate active search results
- restored full smoke-suite pass status for the right-pane search-routing flow

### Broader preview support (Markdown and Syntax Highlighting)

- Added rich text preview support using `FlowDocument` and `RichTextBox` for Markdown and Code.
- Implemented a dependency-free Markdown renderer for bold, headers, and lists.
- Implemented regex-based syntax highlighting for C#, XML, JSON, and Python.
- Enhanced `UpdatePreview` to detect and route to rich previews for relevant extensions.

### Horizontal bookmarks bar

- added a dedicated bookmarks bar below the address area for one-click access to top-level bookmarks
- added a separate View-menu toggle and persisted visibility setting so the bar can be shown independently from the sidebar bookmark tree
- added drag/drop folder bookmarking and direct rename/delete actions from the bar itself

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

The next concrete roadmap slices are now:

### 7. Tab-management parity follow-up
Status: COMPLETE
Scope:
- completed: `021-undo-close-tab`
- rationale: restore flow is now present with menu/context-menu/shortcut access and smoke coverage for single-close, multi-close, and metadata restore cases

### 8. Interaction-level smoke hardening
Status: IN PROGRESS
Scope:
- next: `022-wpf-interaction-smoke-tests`
- rationale: the current smoke suite is strong at viewmodel/persistence coverage but still misses real WPF hit-testing and mouse-capture regressions

### 6. Final Polished Parity (Cubic Conclusion)
Status: COMPLETE
Scope:
- completed: 016 advanced attribute/date search filter
- completed: 017 group by and manual sorting
- completed: 018 external tools configuration
- completed: 019 drop-stack (virtual collection)
- completed: 020 code review fixes

### 4. Power User Parity (Cubic Original)
Status: COMPLETE
Scope:
- completed: advanced batch rename
- completed: breadcrumb dropdown navigation
- completed: content search (grep-style recursive file scanning)

### 5. Advanced Parity (Cubic Final)
Status: COMPLETE
Scope:
- completed: 015 UI layout manager with persisted named layouts and View-menu switching

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
Status: COMPLETE
Scope:
- completed: bounded file-operation queue history with explicit failure/cancel detail surfaced from the status bar popup

## Constraints And Decisions

- stay on `.NET 8` / WPF with no new NuGet packages unless explicitly approved
- keep all file paths sanitized through `FileSystemService`
- do not delete existing targets before replacement copy/move succeeds (safety first)
- keep settings and bookmark watchers active for multi-instance synchronization

## Known Gotchas

- WPF markup generation can intermittently lock `App.g.cs` or `CubicAIExplorer_MarkupCompile.cache`; rerun the build when it happens
- `IMPLEMENTATION_PLAN.md` and `CONTINUE.md` should be kept in sync when roadmap state changes materially
