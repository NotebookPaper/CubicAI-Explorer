# Continuation Instructions for Next Session

> Last updated: 2026-03-10
> Branch: `master`
> HEAD: current local `master` after the latest verified roadmap slice
> Status: shell-aware display names, known-folder alias navigation, shell-backed type metadata, and Explorer reveal-with-selection behavior are now implemented and verified; next roadmap work remains deeper shell context/interoperability follow-up.

Continue in `C:\dev\CubicAI_rewrite` on `CubicAIExplorer.sln`.

## Status

- Local `master` contains the latest verified roadmap slices in this checkout.
- The branch builds and the smoke harness passes again.
- Specs `001-named-session-manager.md` and `002-richer-filter-search-model.md` are complete in this checkout.
- The next completed roadmap slice after those specs is crowded-tab overflow handling for the main tab strip.
- Remaining untracked paths are mostly local Ralph/tooling folders (`.claude/`, `.cursor/`, `.specify/`, `completion_log/`, `obj_verify/`, helper scripts).

## Completed

- Named session manager:
  - added persisted `NamedSession` records inside `UserSettings`
  - added File > Sessions UI for save-as, update, load, delete, and startup-session selection
  - startup now restores a configured named session before falling back to generic last-state restore
  - smoke coverage now verifies named session save/load/delete/startup behavior
- Richer filter/search model:
  - added `Contains`, `Wildcard`, and `Exact` match modes for inline filters and recursive search
  - saved searches now persist and replay their chosen match mode
  - filter history now persists through settings and is reusable from the main window
  - users can opt into clearing inline filters automatically when changing folders
- Details-column customization:
  - added persisted details-column settings for width, visibility, and order in `UserSettings`
  - the View menu now exposes column show/hide, move-left/right, auto-size, and reset actions
  - both panes rebuild the details view from the saved layout so settings survive restart and view-mode switches
  - smoke coverage now verifies column-layout defaults, normalized saves, and settings-service round-trip persistence
- Tab-management parity:
  - added tab context-menu actions for close tabs to the left and close tabs to the right
  - shared close logic now detaches tab event subscriptions consistently for close-left/right/others flows
  - when a close-left/right action removes the active tab, the clicked tab becomes the active fallback
  - smoke coverage now verifies close-left/right behavior
- Tab reuse follow-up:
  - bookmark `Open in New Tab` now activates an existing tab when that folder is already open
  - bookmark `Open All in Tabs` now only creates tabs for unopened folders and reuses existing ones
  - smoke coverage now verifies both single-bookmark and category open-all reuse flows
- Crowded-tab affordances:
  - the main tab strip now scrolls horizontally when there are more tabs than fit in the window
  - added left/right tab-strip scroll buttons plus a `More Tabs` dropdown listing every open tab
  - active-tab changes now auto-scroll the selected tab back into view after tab-count or window-size changes
  - smoke coverage now verifies the overflow wiring is present in the main window
- Shell-aware display names and known-folder aliases:
  - tab titles, breadcrumbs, recent folders, and new bookmark labels now use Windows shell display names instead of raw path parsing where available
  - the address bar now resolves common aliases such as `Desktop`, `Documents`, `Downloads`, `Pictures`, `Music`, `Videos`, and `Home`
  - address autocomplete now suggests matching known folders in addition to normal filesystem completions
  - smoke coverage now verifies alias navigation, shell display-name routing, and known-folder suggestions
- Shell-backed type metadata:
  - centralized shell file-info lookup so Windows-reported type names now feed the Details `Type` column, preview header, recursive search results, and properties dialog
  - bookmark properties now populate real timestamps, attributes, size, and shell type labels instead of placeholder defaults
  - smoke coverage now verifies shell-backed type descriptions in directory listings, recursive search results, and the properties dialog
- Explorer reveal behavior:
  - `Open in Explorer` now reveals a single selected file or folder instead of always opening the containing folder generically
  - no-selection and multi-selection flows still open the current folder to avoid ambiguous partial selection behavior
  - shell launch logic now routes through `IFileSystemService` so the behavior is testable and stays out of window code-behind
  - smoke coverage now verifies that the selected item path is the one sent to Explorer
- Smoke harness cleanup:
  - hardened bookmark watcher callbacks for headless execution without a WPF `Application`
  - refreshed brittle smoke assertions around tab counts and current XAML wiring

## Next Steps

1. Continue deeper shell integration.
   - review shell-context and remaining Explorer interop edge cases
   - decide whether multi-select reveal should stay folder-fallback or move to a deeper shell API later
2. Add smoke-test coverage for the remaining risky file-operation paths:
   - replace failure behavior
   - same-folder duplicate behavior
   - undo/redo after duplicate, new file, and link creation
3. Make settings/bookmark sync more reliable on first run:
   - ensure watcher directories exist before `FileSystemWatcher` setup, or create watchers lazily after the first save
   - harden watcher callbacks against transient `IOException` / partial-write races for both settings and bookmarks

## Key Files

- `src/CubicAIExplorer/Models/DetailsColumnId.cs`
- `src/CubicAIExplorer/Models/DetailsColumnSetting.cs`
- `src/CubicAIExplorer/Models/NamedSession.cs`
- `src/CubicAIExplorer/Models/NameMatchMode.cs`
- `src/CubicAIExplorer/MainWindow.xaml`
- `src/CubicAIExplorer/MainWindow.xaml.cs`
- `src/CubicAIExplorer/Services/IFileSystemService.cs`
- `src/CubicAIExplorer/Services/FileSystemService.cs`
- `src/CubicAIExplorer/Services/ShellFileInfoHelper.cs`
- `src/CubicAIExplorer/Models/UserSettings.cs`
- `src/CubicAIExplorer/Models/SavedSearchItem.cs`
- `src/CubicAIExplorer/ViewModels/MainViewModel.cs`
- `src/CubicAIExplorer/ViewModels/FileListViewModel.cs`
- `tests/CubicAIExplorer.SmokeTests/Program.cs`

## Worktree

Tracked worktree state:

- Explorer reveal-with-selection behavior and its smoke coverage are local and uncommitted in this checkout
- planning/history/spec docs were refreshed to keep roadmap state aligned with the current implementation

Untracked local-only paths:

- `.claude/`
- `.cursor/`
- `.specify/`
- `completion_log/`
- `history/`
- `scripts/lib/`
- `scripts/ralph-loop-codex.ps1`
- `scripts/ralph-loop-codex.sh`
- `scripts/ralph-loop-copilot.sh`
- `scripts/ralph-loop-gemini.sh`
- `scripts/ralph-loop.sh`
- `specs/`
- `src/CubicAIExplorer/obj_verify/`
- `tests/CubicAIExplorer.SmokeTests/obj_verify/`

## Verification

Verification run on the updated checkout on 2026-03-10:

- `dotnet build CubicAIExplorer.sln -v minimal`
  - passed
- `dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal`
  - passed
- `tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe`
  - passed

## Gotchas

- WPF markup generation can intermittently lock `App.g.cs` or `CubicAIExplorer_MarkupCompile.cache`; rerun the build if that happens.
- Do not recreate the `GridView` during tab initialization.
- Avoid keyed `DataTemplate` plus `DataType` combinations in `App.xaml`.
- Do not enable WinForms just to get a folder picker.
- Keep all path handling routed through `FileSystemService` sanitization helpers.
- `FileTransferCollisionResolution.Replace` is currently unsafe because it deletes the destination before the incoming transfer succeeds; do not ship that behavior unchanged.
