# 009 - Empty Recycle Bin

## Status

COMPLETE

## Summary

Add an app-level command to empty the Windows Recycle Bin so CubicAI Explorer can handle a common shell-maintenance task without forcing the user back to Explorer.

## Problem

The rewrite already deletes files to the Recycle Bin by default, but it does not expose any way to manage that accumulated trash from inside the app. Users still need to leave the workflow and switch to Explorer just to empty the bin.

## Goals

- expose an `Empty Recycle Bin` action from the main app menu
- route the operation through the existing filesystem service abstraction
- confirm the action in the window layer and keep the command itself smoke-testable

## Non-Goals

- recycle-bin browsing or per-drive capacity reporting
- restore-from-recycle-bin workflows
- custom shell-verb execution for arbitrary files

## Functional Requirements

1. The Tools menu must expose an `Empty Recycle Bin...` action.
2. Triggering the action must prompt for confirmation before invoking the destructive operation.
3. The destructive operation must be implemented by the Windows shell through `IFileSystemService`, not by manually deleting hidden recycle-bin folders.
4. On success, the main status text must acknowledge that the recycle bin was emptied.
5. Smoke coverage must verify that the command invokes the service abstraction and updates status text.

## Acceptance Criteria

- The user can choose `Tools > Empty Recycle Bin...`, confirm, and the app invokes the shell recycle-bin empty operation.
- The implementation remains testable in the smoke harness without modal UI.
- Build and smoke verification pass after the change.

## Verification Commands

```bash
dotnet build CubicAIExplorer.sln -v minimal
dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal
tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe
```

<!-- NR_OF_TRIES: 1 -->
