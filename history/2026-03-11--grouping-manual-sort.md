Implemented spec 017 by moving file-list sort state into `FileListViewModel` instead of relying on ad hoc `CollectionView` header sorting in the window layer.

Key decisions:
- Grouping uses WPF `PropertyGroupDescription` over computed `FileSystemItem` labels so the same backing collection works in both Details and Tiles views.
- Manual ordering is persisted per folder in `UserSettings.ManualSortFolders`, keyed by folder path and stored as ordered item names. This keeps persistence inside the existing settings pipeline and avoids introducing a new sidecar format or service.
- Entering manual sort clears grouping automatically; combining freeform drag ordering with visual groups would create ambiguous drop behavior and weak persistence semantics.

Lessons:
- WPF smoke/build commands must be run serially when the smoke executable is launched from the same output directory, otherwise the test assembly can lock `CubicAIExplorer.dll`.
- Group order has to be reflected in the underlying item ordering, not only in the `GroupDescriptions`, or date buckets like `Today` / `Earlier this week` can render in an unexpected sequence.
