Implemented spec 016 by extending the existing recursive search workflow instead of introducing a separate search service abstraction. The advanced criteria stay inside `FileListViewModel`, which keeps search behavior aligned with the current pane state and existing saved-search plumbing.

Key decisions:
- treated the `Hidden` and `System` checkboxes as real attribute filters, not just crawl-inclusion toggles, while still allowing hidden traversal when the hidden filter is active
- normalized max-date filtering to the end of the selected day so `DatePicker` ranges behave inclusively
- added shared size/date validation helpers so live search and saved-search persistence use the same rules
- restored advanced criteria through saved-search replay and reopened the advanced row when those criteria are present

Validation notes:
- added smoke coverage for hidden, read-only, size-range, inclusive date-range, and combined name/content/attribute searches
- added saved-search round-trip coverage for advanced criteria
- fixed archive extraction to create missing destination folders so the full smoke suite remains green
