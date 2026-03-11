# Multi-Select Explorer Reveal

- Date: 2026-03-11
- Scope: deeper Explorer interop for the existing `Open in Explorer` command

Implemented `IFileSystemService.RevealInExplorer(IEnumerable<string>)` so the main view model can pass the full file-list selection into the service instead of collapsing multi-select to a generic folder open.

`FileSystemService` now uses `SHParseDisplayName`, `ILFindLastID`, and `SHOpenFolderAndSelectItems` to ask Explorer to highlight multiple selected items in the same parent folder. The service still sanitizes every path, de-duplicates the selection, and falls back to single-item reveal if shell selection cannot be resolved.

Smoke coverage now verifies both the existing single-selection path and the new multi-selection path through the `Open in Explorer` command. This keeps the deeper shell interop slice covered without depending on an interactive Explorer process inside the harness.
