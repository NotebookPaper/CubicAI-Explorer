# 001 - Named Session Manager

## Status

COMPLETE

## Summary

Add explicit named session management to CubicAI Explorer so users can save, load, update, and delete named workspace sessions instead of relying only on automatic state persistence.

This is a parity-focused feature. The original CubicExplorer exposed session workflows as a first-class part of the product. The rewrite already restores tabs and pane state automatically, but it still lacks the explicit session-management layer that makes the app feel like a workspace-oriented file manager rather than only a stateful browser.

## Problem

The current rewrite persists window state, active tabs, and pane paths in settings, but users cannot:

- save the current workspace as a named session
- load a previously saved session on demand
- update an existing named session intentionally
- delete stale sessions
- choose a default startup session explicitly

This makes the current product weaker than the original for users who work with recurring task-based tab sets.

## Goals

- introduce named sessions as a distinct persisted concept
- preserve the existing automatic restore behavior unless a named startup session is chosen
- keep the implementation simple and aligned with the current MVVM/settings architecture
- make the feature usable from the main UI without requiring direct file editing

## Non-Goals

- importing legacy CubicExplorer session files in this spec
- syncing sessions through a separate cloud backend beyond the existing settings file approach
- adding advanced session history or snapshots in this first version
- redesigning the whole settings system

## User Stories

- As a user, I can save my current set of tabs and pane paths as a named session.
- As a user, I can load a saved session later and get the same workspace back.
- As a user, I can update an existing session after rearranging my workspace.
- As a user, I can delete sessions I no longer need.
- As a user, I can choose whether the app starts from auto-restored state or from a specific saved session.

## Functional Requirements

1. Add a persisted named-session model that stores at least:
   - session name
   - open tab paths
   - active tab index
   - right-pane path when dual-pane mode is active
   - whether dual-pane mode is enabled
2. Named sessions must be stored through the repo's existing settings/persistence flow rather than a brand-new storage mechanism unless there is a clear code-level reason not to.
3. The app must expose commands to:
   - save current session as new
   - update/overwrite an existing named session
   - load a selected session
   - delete a selected session
4. The app must expose a minimal session-management UI from the main window.
5. Startup behavior must support:
   - existing automatic restore behavior when no startup session is configured
   - launching into a chosen named session when one is configured
6. Loading a named session must replace the current working tab/pane session cleanly rather than merging state unpredictably.
7. Session names must be validated and trimmed. Empty names must not be saved.
8. Persistence failures must not crash the app; they should fail safely and surface an actionable error when appropriate.

## Implementation Constraints

- Stay on the current `.NET 8` / WPF / MVVM stack.
- Keep the implementation within existing service and settings patterns.
- Do not introduce new NuGet packages.
- Do not break existing automatic tab restore for users who do not use named sessions.
- Keep docs current if product behavior changes materially.

## Likely Files

- `src/CubicAIExplorer/Models/UserSettings.cs`
- `src/CubicAIExplorer/Services/SettingsService.cs`
- `src/CubicAIExplorer/ViewModels/MainViewModel.cs`
- `src/CubicAIExplorer/MainWindow.xaml`
- `src/CubicAIExplorer/MainWindow.xaml.cs`
- `tests/CubicAIExplorer.SmokeTests/Program.cs`
- `IMPLEMENTATION_PLAN.md`
- `CONTINUE.md`

## Acceptance Criteria

1. A user can save the current workspace as a named session from the app UI.
2. A saved session appears in the UI and persists across app restart.
3. Loading a saved session restores:
   - the saved left-pane tabs
   - the saved active tab
   - the saved right-pane path and dual-pane state when present
4. A user can overwrite an existing named session intentionally without creating duplicate ambiguous records.
5. A user can delete a named session from the UI and it is removed from persisted state.
6. If no named startup session is configured, the existing auto-restore flow still works as before.
7. If a named startup session is configured, app launch restores that named session instead of generic last-state restore.
8. `dotnet build CubicAIExplorer.sln -v minimal` passes.
9. `dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal` passes.
10. The smoke-test harness includes coverage for:
    - saving a named session
    - loading a named session
    - deleting a named session
    - startup selection behavior

## Verification Commands

```bash
dotnet build CubicAIExplorer.sln -v minimal
dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal
tests/CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe
```

## Completion Signal

Only output `<promise>DONE</promise>` when all acceptance criteria are verified and the session-manager workflow is actually usable from the app.

<!-- NR_OF_TRIES: 1 -->
