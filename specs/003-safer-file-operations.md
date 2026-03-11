# 003 - Safer File Operations

## Status

COMPLETED

## Summary

Improve the safety of file operations by implementing a "stage-and-rename" or "atomic-swap" style replace flow that prevents data loss if a replace operation fails midway, especially for directories.

## Problem

Currently, `FileTransferCollisionResolution.Replace` implementation in `FileSystemService` uses a backup-and-restore approach:
1. Move the existing destination to a backup path.
2. Perform the copy/move to the original destination path.
3. If it fails, try to move the backup back to the original path.
4. If it succeeds, delete the backup.

While this works for files, it is flawed for directories because if `CopyDirectoryRecursive` fails midway, it will have created a partially populated directory at the original destination path. `Directory.Move` from the backup path back to the original path will then fail because the destination directory already exists (and is not empty). This results in the original directory remaining at the backup path and a partial directory at the destination path, which is not what the user expects.

## Goals

- ensure that the original directory is fully restored if a replace operation fails
- implement a safer replacement flow that minimizes the time the destination is in a partial state
- improve robustness of other risky operations like same-folder duplicates and symbolic link creation

## Non-Goals

- full transactional filesystem support
- multi-device rollback (e.g., across different drives)

## User Stories

- As a user, if I replace a directory and the copy fails halfway, I want my original directory back in its place.
- As a user, if I replace a file and the copy fails, I want my original file back.

## Functional Requirements

1. Modify `FileSystemService.Replace` logic to handle directory restoration properly.
   - For directories, if restoration is needed, the partial directory created during the failed copy must be removed before the backup is moved back.
2. Add smoke-test coverage for:
   - replace failure behavior (both file and directory)
   - same-folder duplicate behavior
   - undo/redo after duplicate, new file, and link creation

## Acceptance Criteria

1. If a directory replace operation fails midway, the original directory is restored to its original path.
2. If a file replace operation fails, the original file is restored.
3. Smoke tests pass for replace failure scenarios.
4. `dotnet build CubicAIExplorer.sln -v minimal` passes.
5. `dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal` passes.
6. `tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe` passes with new coverage.

## Verification Commands

```bash
dotnet build CubicAIExplorer.sln -v minimal
dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal
tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe
```

<!-- NR_OF_TRIES: 1 -->
