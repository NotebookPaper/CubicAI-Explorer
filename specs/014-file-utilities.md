<!-- NR_OF_TRIES: 1 -->

# 014 - File Utilities (Split/Join/Checksum)

## Status: COMPLETE

## Summary

Integrate essential file utility tools into the Tools menu, including a file splitter/joiner and a checksum generator (MD5/SHA1).

## Proposed Changes

### 1. File Splitter/Joiner
- **Split Dialog:** Choose a file and target chunk size (e.g., 10MB, 100MB, 700MB/CD, 4.7GB/DVD).
- **Join Dialog:** Pick the first chunk (e.g., `.001`) and reassemble the original file.
- Implement background task logic in `FileOperationQueueService`.

### 2. Checksum Utility
- Add a "Checksum" tab to the Properties dialog or a standalone "Checksum Tool".
- Generate MD5, SHA1, and SHA256 hashes for selected files.
- Add a "Compare" field to verify a match against a clipboard value.

## Implemented

- Added `Tools > Split File...`, `Join File...`, and `Checksum...` commands routed through `MainViewModel`.
- Added standalone dialogs for split, join, and checksum workflows, with the active selected file prefilled when applicable.
- Extended `IFileSystemService` / `FileSystemService` with chunk splitting, contiguous chunk reassembly, and one-pass MD5/SHA1/SHA256 generation.
- Routed split, join, and checksum execution through the existing file-operation queue so long-running work reports progress and cancellation consistently.
- Added smoke coverage for split/join round-tripping, checksum generation and comparison, and command/menu wiring.

## Acceptance Criteria

- [x] A Tools menu exists with options for Split, Join, and Checksum.
- [x] Large files can be split into numbered chunks.
- [x] Numbered chunks can be reassembled into a bit-perfect original file.
- [x] MD5 and SHA1 checksums can be generated for any file.
- [x] Checksum verification (compare to string) works.
