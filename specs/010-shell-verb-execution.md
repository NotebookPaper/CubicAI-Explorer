# 010 - Shell Verb Execution

## Status: COMPLETE

## Summary

Add explicit shell-verb actions for common alternate launches so CubicAI Explorer can invoke native Windows behaviors such as opening a folder in a new Explorer window or requesting elevation for a selected item.

## Problem

The app already integrates with Explorer for reveal and native properties, but it still lacks an app-owned path for alternate shell verbs. Users must fall back to Explorer or the native shell context menu for straightforward actions like `Run as administrator`.

## Goals

- expose app-owned commands for key alternate shell verbs
- route shell-verb execution through `IFileSystemService`
- keep the command path smoke-testable without invoking real elevated UI

## Non-Goals

- full dynamic enumeration of every registered shell verb
- replacing the native Windows shell context menu
- custom command-line launch flows outside the shell

## Functional Requirements

1. The filesystem abstraction must expose a shell-verb execution method that uses the Windows shell instead of manual command construction.
2. The main app must expose an `Open in New Window` action for the selected folder or current pane path.
3. The main app must expose a `Run as Administrator` action for a single selected item.
4. Success and failure must update app status text without embedding modal UI in the command path.
5. Smoke coverage must verify the requested path and verb for both commands.

## Acceptance Criteria

- A user can trigger `Open in New Window` and CubicAI Explorer requests the Windows `opennewwindow` shell verb for the selected folder or current pane folder.
- A user can trigger `Run as Administrator` and CubicAI Explorer requests the Windows `runas` shell verb for the selected item.
- Build and smoke verification pass after the change.

## Verification Commands

```bash
dotnet build CubicAIExplorer.sln -v minimal
dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal
tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe
```

<!-- NR_OF_TRIES: 1 -->
