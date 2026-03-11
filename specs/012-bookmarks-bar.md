# 012 - Horizontal Bookmarks Bar

## Status

COMPLETE

## Summary

Implement a dedicated horizontal toolbar below the address/breadcrumb bar for one-click access to favorite folders. This mirrors the browser-style bookmarks bar found in the original Cubic Explorer.

## Proposed Changes

### 1. UI Integration
- Add a `BookmarksBar` control in `MainWindow.xaml` between the address bar and the main pane.
- The bar should be toggleable (View menu option).
- Use an `ItemsControl` bound to the top-level bookmarks from `BookmarkService`.

### 2. Behavior
- Clicking a bookmark in the bar navigates the active tab.
- Drag-and-drop support: dragging a folder from the file list to the bar creates a new bookmark.
- Right-click context menu for editing/removing bookmarks directly from the bar.

## Acceptance Criteria

- [x] A horizontal bar is visible below the breadcrumbs.
- [x] Top-level bookmarks are displayed as buttons with icons and labels.
- [x] Clicking a button navigates the current pane to that bookmark's path.
- [x] Users can hide/show the bar via the View menu.
- [x] Adding a bookmark via drag-and-drop to the bar persists the change.
