# 019 - Drop-Stack (Virtual Collection)

<!-- NR_OF_TRIES: 1 -->

## Status: COMPLETE

## Summary

Implement a temporary "shelf" area where users can collect items from various folders before performing a batch copy/move operation without changing anything on disk during collection.

## Proposed Changes

### 1. UI Integration
- Add a "Drop Stack" pane (toggleable sidebar or bottom drawer).
- Display collected items in a list with original paths.

### 2. Collection Logic
- Users drag items from the main file list onto the Drop Stack.
- "Copy all to..." and "Move all to..." buttons on the stack header.
- "Clear stack" button.

## Acceptance Criteria

- [x] A Drop Stack pane is available via the View menu.
- [x] Dragging items into the stack "collects" them (does not move/copy them on disk).
- [x] Users can navigate between folders and continue collecting items.
- [x] Clicking "Copy/Move to..." prompts for a destination and processes all items in the stack.
- [x] Deleting an item from the stack only removes its entry, not the file on disk.
