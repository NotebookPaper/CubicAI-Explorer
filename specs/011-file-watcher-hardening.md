# 011 - File Watcher Hardening

## Status

COMPLETE

## Summary

Harden the external settings and bookmark file watchers so cross-instance sync survives delete/recreate, rename/replace, and watcher-error scenarios instead of only handling plain last-write updates.

## Problem

`SettingsService` and `BookmarkService` currently rely on a narrow `FileSystemWatcher.Changed` path with a simple timestamp debounce. That misses realistic save patterns used by sync tools and editors, including atomic replace flows and file recreation after deletion. It also provides no recovery path when the watcher errors.

## Goals

- keep settings/bookmark cross-instance sync reliable when the backing JSON files are replaced or recreated
- debounce bursty watcher events without dropping legitimate external updates
- recover from watcher errors by re-establishing the watch and reloading state

## Non-Goals

- watching every persisted app data file in this spec
- redesigning settings/bookmark serialization formats
- adding a general-purpose background sync subsystem

## Functional Requirements

1. `SettingsService` must react to external create, change, delete, and rename/replace events for the watched settings file.
2. `BookmarkService` must react to the same external file lifecycle events for the watched bookmark file.
3. Both services must debounce noisy watcher events before reloading.
4. Watcher errors must recreate the watcher and trigger a best-effort reload.
5. Service-owned writes must not immediately bounce back through the external-change event path.
6. Smoke coverage must verify delete/recreate or replace flows for both services.

## Acceptance Criteria

- Recreating `settings.json` after deletion raises `SettingsChanged` with the reloaded payload.
- Replacing `bookmarks.json` via a temp-file move raises `BookmarksChanged` with the new bookmark payload.
- Build and smoke verification pass after the change.

## Verification Commands

```bash
dotnet build CubicAIExplorer.sln -v minimal
dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal
tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe
```

<!-- NR_OF_TRIES: 1 -->
