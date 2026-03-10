## Summary

Added tab-strip context-menu actions for `Close Tabs to the Left` and `Close Tabs to the Right`.

## Decisions

- Reused a shared `CloseTabSet` helper in `MainViewModel` so close-left, close-right, and close-others all detach tab event handlers consistently.
- Promoted the clicked tab to active only when the current active tab was among the tabs being closed; otherwise the existing active tab remains unchanged.
- Kept the feature in the existing tab context menu instead of adding new top-level menu items because the current roadmap gap was tab-strip command parity.

## Verification Notes

- Added smoke coverage for both close-left and close-right flows.
- Extended the XAML wiring smoke assertion so the new click handlers must stay connected.
