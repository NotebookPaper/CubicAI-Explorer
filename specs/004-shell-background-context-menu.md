# 004 - Shell Background Context Menu

## Status: COMPLETE

## Summary

Implement the "background" shell context menu when right-clicking empty space in the file list. This provides parity with Windows Explorer's background menu (View, Sort by, Refresh, Paste, New, Properties) instead of showing the folder's own context menu.

## Problem

Currently, when a user right-clicks on empty space in the file list, the app either shows the default CubicAI Explorer context menu or, if shell integration is enabled, it shows the shell context menu *for the current folder itself*. The latter includes items like "Rename", "Delete", and "Pin to Quick access" which are not appropriate when clicking *inside* a folder. Users expect the Explorer "background" menu which contains "New", "Paste", and "Refresh".

## Goals

- provide a native-feeling background context menu when right-clicking empty space in the file list
- ensure "New", "Paste", and "Refresh" operations from the shell menu work as expected
- maintain parity with the existing `ShellContextMenuHelper`'s support for submenus and shell extensions

## Non-Goals

- replacing the entire file list with a hosted `IShellView` (too large a change for this rewrite stage)
- custom CubicAI items inside the native shell background menu (keep them separate or as a fallback)

## User Stories

- As a user, I can right-click empty space in a folder to see the same "New" and "Paste" options I see in Explorer.
- As a user, I can use the shell's "Refresh" or "Sort by" (though CubicAI has its own sorting) from the background menu.

## Functional Requirements

1. Update `ShellContextMenuHelper` to support retrieving the background context menu for a folder.
   - Use `IShellFolder.CreateViewObject` with `IID_IContextMenu` (or `SHCreateDefaultContextMenu` if appropriate).
2. Update `MainWindow.xaml.cs` to distinguish between clicking an item and clicking the background.
3. When `UseShellContextMenu` is enabled and the background is clicked, show the shell background menu.
4. Ensure the background menu handles commands correctly, especially "New" which might require the window to be a valid drop target or have specific site configuration.

## Implementation Constraints

- Use existing `ShellContextMenuHelper` patterns.
- Do not introduce new NuGet packages.
- Ensure window subclassing for `IContextMenu2/3` still works for the background menu.

## Acceptance Criteria

1. Right-clicking empty space in the file list shows the Explorer background menu when shell integration is enabled.
2. The background menu contains "New", "Paste", and "Properties" (for the current folder).
3. Submenus like "New" work and create items in the current folder.
4. `dotnet build CubicAIExplorer.sln -v minimal` passes.
5. Smoke tests still pass (add a basic check for background menu presence if possible, though native menus are hard to test headlessly).

## Verification Commands

```bash
dotnet build CubicAIExplorer.sln -v minimal
dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal
tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe
```

<!-- NR_OF_TRIES: 1 -->
