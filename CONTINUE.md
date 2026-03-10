# Continuation Instructions for Next Session

> Last updated: 2026-03-10
> Branch: `master`
> HEAD: `a625885` - `Enhance Edit menu with Duplicate, Copy Path options, New File, Invert Selection and Symbolic Link support`
> Status: local `master` was fast-forwarded to `origin/master`; tracked worktree differs only by this refreshed handoff, plus local-only untracked helper/artifact folders

Continue in `C:\dev\CubicAI_rewrite` on `CubicAIExplorer.sln`.

## Status

- Local `master` now matches GitHub at `a625885`.
- The archive/queue/saved-search work from `e636845` is still part of the baseline, but GitHub added 12 more commits on top of it.
- The latest remote slices include:
  - bookmark import/export menu work and XML import fixes
  - hierarchical bookmarks with icons, folders, rename, and drag/drop organization
  - sidebar resizability/layout fixes and better vertical fill behavior
  - multi-machine sync and session persistence via file watchers and non-locking file access
  - address bar moved inline with the toolbar plus a New Tab button in the tab bar
  - Edit menu additions: Duplicate, Copy Path, New File, Invert Selection, and symbolic link support
- `CONTINUE.md` and `IMPLEMENTATION_PLAN.md` on the branch were stale after the GitHub updates; only `CONTINUE.md` has been refreshed in this local checkout so far.
- Remaining untracked paths are local-only (`.claude/` and `obj_verify` folders) and are not part of the shipped project state.

## Completed

- Baseline already shipped before the fast-forward:
  - archive browser and extraction dialogs
  - queue progress/cancel UX
  - saved-search rename behavior
  - README and refreshed planning docs
- Additional GitHub-delivered work now in the checkout:
  - new `GridLengthConverter` and App resource wiring
  - substantial `MainWindow.xaml` / `MainWindow.xaml.cs` layout and menu changes
  - bookmark model expansion in `BookmarkItem`
  - new session/sync-related settings in `UserSettings` and `SettingsService`
  - `MainViewModel` and `FileListViewModel` updates for bookmarks, toolbar/address bar, and new edit actions

## In Progress

- The current branch does not build cleanly after the GitHub fast-forward.
- The first build attempt hit the known WPF markup file-lock issue on `App.g.cs`.
- The second build attempt exposed a real XAML error:
  - [MainWindow.xaml](/C:/dev/CubicAI_rewrite/src/CubicAIExplorer/MainWindow.xaml#L455)
  - `MC3088: Property elements cannot be in the middle of an element's content`
  - The immediate cause is `EventSetter` elements placed after `Style.Triggers` inside the `TreeView.ItemContainerStyle`.

## Next Steps

1. Recover the build before any further feature work:
   - fix the invalid `TreeView.ItemContainerStyle` ordering in [MainWindow.xaml](/C:/dev/CubicAI_rewrite/src/CubicAIExplorer/MainWindow.xaml#L446)
   - remove the duplicate `NavigateRequested` event in [FileListViewModel.cs](/C:/dev/CubicAI_rewrite/src/CubicAIExplorer/ViewModels/FileListViewModel.cs#L81)
   - reconcile the new file/link commands with the service abstraction by adding `CreateFile` / `CreateSymbolicLink` to [IFileSystemService.cs](/C:/dev/CubicAI_rewrite/src/CubicAIExplorer/Services/IFileSystemService.cs) and implementing them in [FileSystemService.cs](/C:/dev/CubicAI_rewrite/src/CubicAIExplorer/Services/FileSystemService.cs), or temporarily backing the commands out
2. Once the branch builds, verify the latest Edit-menu slice end to end:
   - `dotnet build CubicAIExplorer.sln -v minimal`
   - `dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal`
   - manually exercise Duplicate, Copy Path/Name, New File, Invert Selection, and Create Symbolic Link in both panes
3. Fix the replace-on-conflict data-loss path in [FileSystemService.cs](/C:/dev/CubicAI_rewrite/src/CubicAIExplorer/Services/FileSystemService.cs#L599):
   - do not delete the existing target before the replacement copy/move succeeds
   - prefer a stage/rename-backup flow so failed replacements preserve the original destination
4. Add smoke-test coverage for the risky file-operation paths:
   - replace failure behavior
   - same-folder duplicate behavior
   - undo/redo after duplicate, new file, and link creation
5. Fix startup/session initialization:
   - remove the unconditional extra tab creation in [App.xaml.cs](/C:/dev/CubicAI_rewrite/src/CubicAIExplorer/App.xaml.cs#L41)
   - keep [MainViewModel.cs](/C:/dev/CubicAI_rewrite/src/CubicAIExplorer/ViewModels/MainViewModel.cs#L224) as the single owner of restore-vs-create tab logic
6. Make settings/bookmark sync reliable on first run:
   - ensure watcher directories exist before `FileSystemWatcher` setup, or create watchers lazily after the first save
   - harden watcher callbacks against transient `IOException` / partial-write races for both settings and bookmarks
7. After the code is stable again, refresh `IMPLEMENTATION_PLAN.md` so it matches the post-`a625885` reality rather than the older archive/queue milestone.

## Key Files

- `src/CubicAIExplorer/App.xaml`
- `src/CubicAIExplorer/Converters/GridLengthConverter.cs`
- `src/CubicAIExplorer/MainWindow.xaml`
- `src/CubicAIExplorer/MainWindow.xaml.cs`
- `src/CubicAIExplorer/Models/BookmarkItem.cs`
- `src/CubicAIExplorer/Models/UserSettings.cs`
- `src/CubicAIExplorer/Services/SettingsService.cs`
- `src/CubicAIExplorer/ViewModels/MainViewModel.cs`
- `src/CubicAIExplorer/ViewModels/FileListViewModel.cs`
- `tests/CubicAIExplorer.SmokeTests/Program.cs`

## Worktree

Tracked worktree state:

- `CONTINUE.md` modified locally for this handoff refresh

Untracked local-only paths:

- `.claude/`
- `src/CubicAIExplorer/obj_verify/`
- `tests/CubicAIExplorer.SmokeTests/obj_verify/`

## Verification

Verification run on the updated checkout on 2026-03-10:

- `dotnet build CubicAIExplorer.sln -v minimal`
  - failed once with the known WPF markup lock on `App.g.cs`
- `dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal`
  - failed with `MC3088` in [MainWindow.xaml](/C:/dev/CubicAI_rewrite/src/CubicAIExplorer/MainWindow.xaml#L455)

No smoke-test executable run was attempted after the fast-forward because the project currently fails to build.

## Gotchas

- WPF markup generation can intermittently lock `App.g.cs` or `CubicAIExplorer_MarkupCompile.cache`; rerun the build if that happens.
- Do not recreate the `GridView` during tab initialization.
- Avoid `DockPanel` for file-list plus popup layouts; explicit `Grid` has been safer here.
- Avoid keyed `DataTemplate` plus `DataType` combinations in `App.xaml`.
- Do not enable WinForms just to get a folder picker.
- Keep all path handling routed through `FileSystemService` sanitization helpers.
- The current top priority is the invalid `Style` content ordering in `MainWindow.xaml`; until that is fixed, the updated branch is not buildable.
- After the markup fix, expect additional compile errors from the duplicate `NavigateRequested` declaration and the missing `CreateFile` / `CreateSymbolicLink` members on `IFileSystemService`.
- `FileTransferCollisionResolution.Replace` is currently unsafe because it deletes the destination before the incoming transfer succeeds; do not ship that behavior unchanged.
