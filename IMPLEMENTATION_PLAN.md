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
- bookmarks, recent folders, breadcrumbs, autocomplete, search/filter, and recursive search
- preview panel with text, image, folder, metadata, and fallback states
- properties dialog, toolbar, context menus, keyboard shortcuts, and persisted window/settings state
- smoke-test harness covering core behaviors and recent regressions

## Recently Completed Slices

### Transfer reliability

- conflict-aware paste flow supports `KeepBoth`, `Replace`, and `Skip`
- transfer history keeps partial results instead of collapsing mixed outcomes
- skip-only results report through pane status text rather than modal noise
- clipboard handling is more robust against transient failures and Explorer drop-effect variants

Primary files:

- `src/CubicAIExplorer/ViewModels/FileListViewModel.cs`
- `src/CubicAIExplorer/Services/FileSystemService.cs`
- `src/CubicAIExplorer/Services/IFileSystemService.cs`
- `src/CubicAIExplorer/Services/ClipboardService.cs`
- `src/CubicAIExplorer/Views/FileConflictDialog.xaml`
- `src/CubicAIExplorer/Views/FileConflictDialog.xaml.cs`

### Background file operations

- added a shared `IFileOperationQueueService` / `FileOperationQueueService`
- paste, delete, permanent delete, and drag/drop transfers run off the UI thread
- queue state is surfaced in the status bar
- active queued transfers now expose item-count progress and cooperative cancellation
- the queue details panel now shows current progress/detail plus a cancel action

Primary files:

- `src/CubicAIExplorer/Services/IFileOperationQueueService.cs`
- `src/CubicAIExplorer/Services/FileOperationQueueService.cs`
- `src/CubicAIExplorer/App.xaml.cs`
- `src/CubicAIExplorer/MainWindow.xaml`
- `src/CubicAIExplorer/MainWindow.xaml.cs`
- `src/CubicAIExplorer/ViewModels/TabViewModel.cs`

### Preview and archive support

- small binary files fall back to a hex preview
- image preview includes dimensions and basic metadata
- ZIP files show archive metadata and entry listings in preview
- added `Extract Archive` for single selected `.zip` files
- added an in-app archive browser with filtering and a folders-only toggle for larger ZIPs

Primary files:

- `src/CubicAIExplorer/ViewModels/MainViewModel.cs`
- `src/CubicAIExplorer/ViewModels/FileListViewModel.cs`
- `src/CubicAIExplorer/Services/FileSystemService.cs`
- `src/CubicAIExplorer/Services/IFileSystemService.cs`
- `src/CubicAIExplorer/MainWindow.xaml`

### Saved searches and cleanup

- saved searches persist and render in the left rail
- running a saved search rehydrates the search results view
- `MainViewModel` command-forwarding/property-notification duplication was reduced

Primary files:

- `src/CubicAIExplorer/Models/SavedSearchItem.cs`
- `src/CubicAIExplorer/ViewModels/MainViewModel.cs`
- `src/CubicAIExplorer/ViewModels/FileListViewModel.cs`
- `src/CubicAIExplorer/MainWindow.xaml`
- `src/CubicAIExplorer/MainWindow.xaml.cs`

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
- image preview metadata
- saved search persistence and rerun behavior
- preview refresh and preview status states

## Next Planned Work

1. Manual UX pass on the newest slices:
   - paste conflict dialog behavior
   - transfer summary wording
   - queue visibility and busy feedback
   - ZIP extraction flow
   - saved-search rail interactions
2. Archive follow-up:
   - add richer archive actions beyond browse + filter + extract-all
   - decide whether opening an archive should stay metadata-first or become navigable
3. Preview follow-up:
   - extend metadata/preview support for more file types
   - keep expensive preview generation off the UI thread
4. Queue follow-up:
   - preserve richer queue history than the last result only
   - consider per-item failure detail or batch summaries beyond the current item-count progress model

## Constraints And Decisions

- stay on `.NET 8` / WPF with no new NuGet packages unless explicitly approved
- keep all file paths sanitized through `FileSystemService`
- prefer existing MVVM and service-wiring patterns over new abstractions
- do not recreate the `GridView` during tab initialization
- avoid `DockPanel` for file-list plus popup layouts; use explicit `Grid`
- avoid keyed `DataTemplate` plus `DataType` combinations in `App.xaml`
- do not enable WinForms just to obtain folder dialogs

## Known Gotchas

- WPF markup generation can intermittently lock `App.g.cs` or `CubicAIExplorer_MarkupCompile.cache`; rerun the build when it happens
- `IMPLEMENTATION_PLAN.md` and `CONTINUE.md` should be kept in sync when roadmap state changes materially
