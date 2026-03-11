## 2026-03-11 - Layout Manager

- Stored named window layouts in `settings.json` alongside sessions instead of introducing a separate persistence file, which kept cross-instance settings sync and watcher behavior unchanged.
- Captured the layout state the app already knows how to apply safely: sidebar section visibility/width, bookmarks bar visibility, preview visibility/width, dual-pane mode, and file-list view mode.
- Added a small manage-layouts dialog for apply/delete workflows rather than building a heavier preferences surface; saving still reuses the existing simple name-entry dialog pattern.
- Hooked preview-width persistence through the preview splitter so saved layouts restore the size the user actually chose instead of an unused default width value.
