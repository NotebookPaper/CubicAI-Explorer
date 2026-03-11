Implemented spec 013 by extending persisted tab/session state with lock and color metadata while keeping the legacy `OpenTabs` path list for backward compatibility.

Key behavior decisions:
- Locking a tab captures its current folder as the lock root and allows in-place navigation only within that folder subtree.
- Attempts to navigate outside the locked subtree fork into a new tab instead of mutating the locked tab.
- Back/forward navigation follows the same rule by peeking at history before applying the navigation step.

UI notes:
- Added a tab context-menu lock toggle plus a fixed color palette with clear-color support.
- Tab headers render a subtle color wash plus an accent strip, and the lock indicator appears on the active locked tab.

Validation:
- Added smoke coverage for locked-tab fork navigation and persistence across settings reload.
- Verified solution build, smoke-test project build, and the full smoke harness.
