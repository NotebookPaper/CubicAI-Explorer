# Safer File Operations (Stage-and-Rename)

- Date: 2026-03-11
- Scope: File system reliability and atomic-like operations

Improved the safety of file replace operations by implementing a "stage-and-rename" flow. Instead of overwriting files/directories directly (which can leave them in a corrupted state if the operation fails halfway), we now:
1. Copy/Move the item to a temporary path.
2. Create a backup of the existing destination.
3. Perform a quick swap of the new item into the destination path.
4. Delete the backup on success, or restore it on failure.

This specifically fixes issues where directory replacement would leave a "partial" directory if a copy error occurred, preventing a full restoration of the original directory.

Key changes:
- Refactored `FileSystemService` to use `PerformStageAndRename` for all replace-style copies and moves.
- Added comprehensive smoke-test coverage for replace failure scenarios (simulated by locking files/directories).
- Added smoke-test coverage for same-folder duplicates and undo/redo of new file/link creation.
