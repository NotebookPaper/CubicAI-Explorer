# 007 - Bookmark Drag Feedback

## Status

COMPLETE

## Summary

Improve bookmark drag-and-drop usability by surfacing clear visual feedback while the user reorders bookmark entries or moves them between categories.

## Problem

Bookmark reordering currently works, but the sidebar provides very little feedback during the drag operation. Users cannot easily tell:
- whether the current drop target is valid
- whether a folder will receive the bookmark as a child
- when the drop will move the bookmark back to the top level

That ambiguity makes a relatively destructive interaction feel brittle.

## Proposed Changes

### 1. Drag Hint Text
- Show a short inline hint above the bookmark tree while a bookmark drag is active.
- Valid hints should explain whether the drop moves into a folder, after a bookmark, or back to the top level.
- Invalid drops should show a concise rejection message.

### 2. Visual Target Highlighting
- Highlight the active bookmark drop target in the tree.
- Highlight the bookmark tree surface when dropping to the top level.
- Clear all transient highlight state when the drag ends or leaves the tree.

### 3. Validation Coverage
- Add smoke coverage for bookmark drag-feedback state transitions.
- Extend existing XAML wiring checks to cover the new bindings and drag-leave handler.

## Acceptance Criteria

- [x] Dragging a bookmark over a folder shows a hint that the bookmark will move into that folder.
- [x] Dragging a bookmark over empty bookmark-tree space shows a hint that the bookmark will move to the top level.
- [x] Invalid drops onto the dragged item itself or one of its descendants show a rejection hint and do not highlight the target as valid.
- [x] Visual drop-target state is cleared when the drag completes or leaves the bookmark tree.
- [x] Smoke tests verify bookmark drag-feedback state and XAML wiring.

<!-- NR_OF_TRIES: 1 -->
