<!-- NR_OF_TRIES: 1 -->

# 010 - Breadcrumb Dropdown Navigation

## Status: COMPLETE

## Summary

Enhance the breadcrumb navigation bar with interactive dropdown buttons between path segments. Each dropdown lists subfolders of the preceding segment, allowing quick branching.

## Proposed Changes

### 1. Breadcrumb UI
- Update `BreadcrumbSegment` and its visual representation in `MainWindow.xaml`.
- Add a small dropdown button (`v` icon) to the right of each path segment.
- Clicking the dropdown should populate a `ContextMenu` or `Popup` with immediate subfolders of that segment's path.

### 2. Navigation
- Selecting an item from the dropdown navigates the current tab to that subfolder.
- The dropdown should be populated asynchronously to prevent UI hangs on slow drives.

## Acceptance Criteria

- [x] Every breadcrumb segment (except the last) has a dropdown button.
- [x] Clicking a dropdown shows a list of subdirectories for that segment.
- [x] Selecting a subdirectory from the list performs a navigation to that path.
- [x] Navigation via dropdown preserves the back/forward history.
