# 002 - Richer Filter And Search Model

## Status: COMPLETE

## Summary

Strengthen the file-list narrowing workflow so inline filters, recursive search, and saved searches expose explicit matching semantics and reusable history instead of only a basic substring match.

## Problem

The rewrite already supports inline filtering, recursive search, and saved searches, but all three feel lighter than the original CubicExplorer workflow:

- filter semantics are implicit and limited to substring matching
- recursive search cannot preserve wildcard or exact-match intent
- users cannot quickly reuse common file-list filters
- filters linger across navigation even when the user wants per-folder narrowing

## Goals

- add explicit match modes shared across filter and search flows
- persist filter history for quick reuse
- keep saved searches faithful to the chosen search mode
- support optional clear-on-folder-change filter behavior

## Non-Goals

- full advanced search syntax beyond filename-oriented exact/wildcard/contains matching
- content indexing or file-content search
- a new standalone settings UI for filter/search preferences

## Functional Requirements

1. Inline filtering must support `Contains`, `Wildcard`, and `Exact` name matching.
2. Recursive search must support the same explicit matching modes.
3. Saved searches must persist and replay their selected search mode.
4. Filter history must be reusable from the main UI and persisted through the existing settings flow.
5. Users must be able to opt into clearing inline filters automatically when the folder changes.

## Acceptance Criteria

1. Inline filter mode changes alter matching behavior without reopening the tab.
2. Recursive search mode changes alter search results and saved searches replay with the same semantics.
3. Filter history persists through settings and applies to new tabs.
4. When clear-on-folder-change is enabled, navigating to another folder clears the inline filter.
5. `dotnet build CubicAIExplorer.sln -v minimal` passes.
6. `dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal` passes.
7. `tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe` passes with coverage for filter modes, search modes, saved-search mode persistence, and filter history/navigation behavior.

## Verification Commands

```bash
dotnet build CubicAIExplorer.sln -v minimal
dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal
tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe
```

<!-- NR_OF_TRIES: 1 -->
