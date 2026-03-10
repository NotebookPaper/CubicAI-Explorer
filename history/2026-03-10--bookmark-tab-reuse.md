## Summary

Added bookmark-driven tab reuse so opening a bookmarked folder in a new tab activates an existing tab for that folder when one is already open.

## Decisions

- Scoped reuse to bookmark workflows that explicitly create tabs instead of changing generic in-place navigation semantics.
- Reused the same behavior for `Open All in Tabs` by routing it through the bookmark new-tab helper.
- Kept `Duplicate Tab` unchanged because that command is intentionally allowed to create a second tab for the same folder.

## Verification Notes

- Added smoke coverage for single-bookmark reuse and category `Open All in Tabs` reuse.
- Verified that bookmark-driven reuse still activates the appropriate resulting tab.
