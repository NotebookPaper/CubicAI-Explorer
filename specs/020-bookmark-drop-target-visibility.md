# 020 - Bookmark Drop Target Visibility & Precision

## Status: COMPLETE

## Summary

Fix a regression where bookmark folders become non-interactive or invisible during drag-and-drop operations, preventing users from dropping bookmarks into specific subfolders.

## Problem

Recent drag-feedback updates improved visual cues but introduced a limitation:
- Bookmark folders do not consistently accept drops during a drag operation.
- The tree view may hide or collapse folders when a drag starts, or simply fail to provide a "drop into" target area for nested folders.
- This makes reorganizing a deep bookmark hierarchy impossible.

## Proposed Changes

### 1. Drop Target Logic
- Update the `BookmarkService` or the Sidebar UI logic to explicitly distinguish between "drop above/below" (sibling) and "drop onto" (child).
- Ensure that every `BookmarkItem` that `IsFolder == true` remains a valid drop target during a drag.

### 2. TreeView Interaction
- Ensure `TreeViewItem` controls for bookmark folders do not change visibility or hit-testability when a drag is active.
- Re-enable the "hover to expand" behavior during drag-drop so users can navigate into subfolders while holding a bookmark.

### 3. Visual Feedback
- Refine the Drag Hint text:
  - "Move into [FolderName]" when hovering directly over a folder.
  - "Move after [BookmarkName]" when hovering between items.
- Ensure the highlight clearly covers the entire folder row when a "drop into" is valid.

## Acceptance Criteria

- [x] Dragging a bookmark over a folder correctly identifies the folder as a "drop into" target.
- [x] Hovering over a collapsed bookmark folder for a short duration during a drag expands the folder.
- [x] Dropping a bookmark onto a folder correctly adds it as a child of that folder.
- [x] Drag hint text accurately distinguishes between sibling and child drops.
- [x] Smoke tests verify the ability to drop into nested bookmark folders.

<!-- NR_OF_TRIES: 1 -->
