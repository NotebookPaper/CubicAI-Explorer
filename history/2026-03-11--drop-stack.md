## Spec 019 - Drop Stack

- Implemented the feature as a toggleable sidebar pane instead of a floating window so it fits the existing WPF shell without introducing more layout state than the spec requires.
- Kept collected entries session-local and non-destructive: dragging to the pane only records paths, duplicate paths are ignored, and removing an entry never touches disk.
- Reused the existing filesystem abstraction plus file-operation queue for `Copy all to...` and `Move all to...`, which keeps sanitization, queue progress, and refresh behavior consistent with the rest of the app.
- Copy operations intentionally leave the collected entries in place for reuse; successful move operations remove transferred entries so the shelf does not retain stale source paths.
- Smoke coverage now verifies cross-folder collection persistence, non-destructive removal, bulk copy, bulk move, and XAML/code-behind wiring for the pane.
