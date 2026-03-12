Completed spec 020 bookmark drop target visibility and precision.

- Added a `BookmarkDropPlacement` model so bookmark drags now classify row-center folder hovers as child drops and edge hovers as sibling drops.
- Extended `BookmarkItem` with separate `IsDropIntoTarget` and `IsDropAfterTarget` flags so the tree can render full-row child highlights and distinct sibling insertion cues.
- Reworked the bookmark drag handling in `MainWindow` to keep the tree interactive during drag, compute drop targets from row hit-testing, and auto-expand collapsed folders after a short hover.
- Updated smoke coverage to verify nested-folder drops, drag hint text for both placement modes, and XAML/code-behind wiring for the hover-expand path.
