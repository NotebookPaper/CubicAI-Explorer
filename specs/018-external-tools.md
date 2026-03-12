# 018 - External Tools Configuration

<!-- NR_OF_TRIES: 1 -->

## Status: COMPLETE

## Summary

Enable users to integrate their favorite third-party tools (e.g., editors, diff tools) directly into the file explorer's interface, allowing them to open files with specific programs.

## Proposed Changes

### 1. Configuration UI
- Add an "External Tools" tab to the Preferences window.
- Allow users to add "Tool" entries: Name, Path, and optional Command-line Arguments (e.g., `%p` for path).

### 2. Tools Menu & Context Menu Integration
- Populated a "Tools" submenu with user-defined entries.
- Add an "Open with... (Tools)" submenu to the right-click context menu.
- Launch tools via `Process.Start` with the selected item's path as an argument.

## Acceptance Criteria

- [x] Users can add custom external programs in the Preferences window.
- [x] Users can set command-line arguments (like `%p`) for each tool.
- [x] Added tools appear in the main Tools menu and right-click context menu.
- [x] Tools launch with the correct selected file as an argument.
