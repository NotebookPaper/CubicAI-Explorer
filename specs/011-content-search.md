<!-- NR_OF_TRIES: 1 -->

# 011 - Content Search (Grep)

## Status

COMPLETE

## Summary

Extend the file explorer's recursive search capabilities to include text-based content matching. This allows users to find files containing specific strings (Grep-style) rather than just filename matches.

## Proposed Changes

### 1. Search Bar Extensions
- Add a "Contains text:" field to the search bar.
- Add an "Include content" toggle in the search options.
- Ensure the user can still search by filename only or combine both.

### 2. Search Engine Update
- Update `FileListViewModel` search logic to read file contents when requested.
- Use a safe, chunk-based approach to scan files and skip non-text extensions or files larger than 10 MB.
- Persist content-search criteria through saved searches so the existing search-results view can replay both filename and content filters.

## Acceptance Criteria

- [x] Users can enter a text string to search for within files.
- [x] Content search is recursive and respects the folder depth.
- [x] The search doesn't hang on large files (e.g., skips files over 10 MB or those without text extensions).
- [x] Search results show the files where the content match was found.
