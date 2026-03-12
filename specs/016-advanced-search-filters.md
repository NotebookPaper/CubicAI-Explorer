<!-- NR_OF_TRIES: 1 -->

# 016 - Advanced Attribute/Date Search Filter

## Status: COMPLETE

## Summary

Extend the search engine to support advanced filtering based on file metadata, including attributes (Hidden, System, etc.), size ranges, and modification dates.

## Proposed Changes

### 1. Search UI
- Add an "Advanced" expandable section to the search bar.
- Include checkboxes for file attributes.
- Add "Date modified" (Range) and "Size" (Min/Max) inputs.

### 2. Search Logic
- Update `SearchService` to apply these filters during the recursive crawl.
- Ensure combined filters (e.g., "Filename contains 'test' AND Size > 10MB AND Attribute != Hidden") work correctly.

## Acceptance Criteria

- [x] Users can filter search results by one or more file attributes.
- [x] Users can specify a minimum and/or maximum file size for search results.
- [x] Users can specify a date range for file modification.
- [x] Advanced filters can be combined with filename and content search.
