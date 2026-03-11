Implemented the first deeper shell-metadata slice without adding new dependencies.

- Added `ShellFileInfoHelper` as the shared `SHGetFileInfo` wrapper for display-name and type-name lookup so shell metadata stops being duplicated inside unrelated classes.
- Routed file, folder, drive, recursive-search, and bookmark-property item creation through shell type-name lookup, while keeping `FileSystemItem.TypeDescription` fallback-safe when shell lookup is unavailable.
- Updated preview headers to use the selected item's resolved type description so preview, Details view, and properties now stay aligned.
- Smoke coverage now verifies that directory listings, recursive search results, and the properties dialog all surface the same shell-reported type description for a known file type.
- Remaining shell roadmap work is context behavior rather than metadata plumbing, especially Explorer reveal / selection interop.
