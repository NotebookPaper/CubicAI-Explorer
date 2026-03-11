## 2026-03-11 - File Utilities

- Added standalone split, join, and checksum dialogs instead of expanding the existing properties dialog, which kept the new workflows focused and avoided overloading a read-mostly window.
- Extended `IFileSystemService` so chunk splitting, chunk joining, and checksum generation stay behind the filesystem abstraction rather than being implemented in the window layer.
- Split output uses numbered suffixes like `.001` and `.002`, refuses to overwrite existing chunk files, and cleans up partial chunk output if the operation fails or is canceled.
- Join requires the first chunk and validates that the full numeric sequence is contiguous before writing the merged output, which avoids silently ignoring gaps.
- Checksum generation computes MD5, SHA1, and SHA256 in a single streaming pass, and the comparison helper normalizes spaces and dashes so pasted values are easy to verify.
