# 013 - Tab Locking and Coloring

<!-- NR_OF_TRIES: 1 -->

## Status: COMPLETE

## Summary

Add support for locking tabs to specific paths and assigning custom visual colors to tab headers. This enhances organizational efficiency for power users managing many open locations.

## Proposed Changes

### 1. Tab Models
- Update `TabItem` to include `IsLocked` (bool) and `TabColor` (Color/Brush).
- Implement persistence for these properties in the `SettingsService`.

### 2. Locking Behavior
- **Locked Tab:** If the user tries to navigate away from a locked path, the navigation happens in a *new* tab instead.
- **Locked with Subdirs:** Allows navigation within the locked folder's descendants but prevents navigating "up" or to unrelated paths.
- Show a "lock" icon on the tab header when active.

### 3. Coloring UI
- Add a "Tab Color" submenu to the Tab context menu.
- Allow selecting from a predefined palette or a custom color picker.
- Apply the color to the tab header background or an indicator strip.

## Acceptance Criteria

- [x] Tabs can be toggled to "Locked" via context menu.
- [x] Navigating away from a locked tab opens a new tab for the destination.
- [x] Tabs can have custom colors assigned via the context menu.
- [x] Tab lock and color states persist across application restarts.
