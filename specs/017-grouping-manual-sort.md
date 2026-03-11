# 017 - Group By and Manual Sorting

<!-- NR_OF_TRIES: 1 -->

## Status

COMPLETE

## Summary

Implement visual grouping for the file list and support for persistent manual item reordering, matching the flexible organization features of Cubic Explorer.

## Proposed Changes

### 1. Visual Grouping
- Add "Group By" submenu to the View menu (Options: Name, Type, Size, Date, None).
- Use WPF `CollectionView` grouping to display visual headers in the file list.

### 2. Manual Sorting
- Allow users to drag-and-drop items to custom positions when the sort mode is set to "Manual".
- Persist manual sort order per folder in a local database or sidecar file (e.g., `folder_meta.json`).

## Acceptance Criteria

- [x] Users can group files by common properties (e.g., "Earlier this week").
- [x] Users can toggle manual sorting mode.
- [x] Dragging items in manual mode reorders them and persists the order for that folder.
- [x] Grouping works in both Details and Tiles view modes.
