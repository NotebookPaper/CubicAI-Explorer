<!-- NR_OF_TRIES: 1 -->

# 009 - Advanced Batch Rename

## Status: COMPLETE

## Summary

Implement a dedicated dialog for multi-file renaming, providing parity with CubicExplorer's powerful batch rename tool. Supports counters, case transformation, and extension manipulation.

## Proposed Changes

### 1. Batch Rename Dialog
- Create a `BatchRenameDialog` that accepts a collection of `FileSystemItem`.
- UI: List of files showing `Original Name` and `New Name` (Live Preview).
- Options:
  - **Find and Replace:** Simple string replacement.
  - **Case:** Lowercase, Uppercase, Title Case, Sentence Case.
  - **Counter:** Append or prepend numbers (e.g., `_01`, `_02`).
  - **Extensions:** Keep, remove, or change extension.

### 2. Rename Logic
- Implement `BatchRenameService` to handle the heavy lifting.
- Ensure the operation is transactional: if one rename fails due to conflict, offer `Keep Both` or `Skip`.
- Integrate with existing `UndoService` so the entire batch can be undone in one step.

## Acceptance Criteria

- [x] Selecting multiple files and pressing the Batch Rename command (or F2 with multiple items) opens the dialog.
- [x] Users can see a live preview of the new filenames as they toggle options.
- [x] Batch rename supports at least Case, Search/Replace, and Counters.
- [x] A single Undo command reverts all files renamed in the batch.
