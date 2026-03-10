Implemented the first deeper shell-integration slice around path/display handling instead of shell context menus.

Key decisions:
- kept path resolution inside `FileSystemService` by adding `ResolveDirectoryPath()` for well-known aliases and `GetDisplayName()` for shell-backed labels
- reused existing `SHGetFileInfo` shell metadata rather than adding a new dependency or a broader shell abstraction
- routed tab titles, breadcrumbs, bookmark labels, recent folders, and address-bar navigation through the existing MVVM flow so behavior stays centralized and testable
- autocomplete returns resolved known-folder paths for alias prefixes instead of introducing a new suggestion view model shape

Lessons:
- shell display names are locale-dependent, so smoke coverage should compare UI surfaces against the service result rather than hard-coded English labels
- alias handling is useful even without a full shell namespace browser because it closes several obvious path-entry gaps with very little surface area

Remaining follow-up:
- expose shell type names and other metadata in details/properties where it improves parity
- review shell-context and Explorer interop behavior beyond path entry and display labels
