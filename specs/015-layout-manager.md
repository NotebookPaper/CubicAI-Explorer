<!-- NR_OF_TRIES: 1 -->

# 015 - UI Layout Manager

## Status

COMPLETE

## Summary

Implement a mechanism for saving and restoring named window layout profiles. This allows the user to quickly switch between UI configurations optimized for different tasks (e.g., "Image Editing", "Side-by-Side File Management").

## Proposed Changes

### 1. Layout Engine
- Define a `WindowLayout` model that captures:
  - Sidebar visibility/width.
  - Preview panel visibility/width/position.
  - Dual pane mode status.
  - Bookmarks bar visibility.
  - Current ViewMode.
- Save these layouts to a dedicated `layouts.json` or within `settings.json`.

### 2. Layout Menu
- Add a "Layouts" submenu to the View menu.
- Options: "Save Layout as...", "Manage Layouts...", and a list of saved layouts.
- Applying a layout should update all relevant UI states immediately.

## Acceptance Criteria

- [x] Users can save the current window configuration as a named layout.
- [x] Users can switch between saved layouts from the View menu.
- [x] UI elements (sidebar, preview, dual pane, etc.) update when a layout is applied.
- [x] Layouts are persisted across application sessions.
