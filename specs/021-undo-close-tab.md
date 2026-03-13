# 021 - Undo Close Tab
<!-- NR_OF_TRIES: 1 -->

## Status: COMPLETE

## Summary

Restore parity with original CubicExplorer by adding an `Undo Close Tab` workflow that reopens the most recently closed tab with its persisted state.

## Problem

The rewrite supports close, close-left, close-right, close-others, duplicate, and persisted tab/session state, but it does not keep a recently-closed-tab stack.

The original CubicExplorer explicitly added `Undo Close Tab`, and this remains one of the more visible parity gaps in tab management.

## Proposed Changes

## Implemented

### 1. Recently Closed Tab Stack

- Track a bounded in-memory stack of recently closed tabs in `MainViewModel`.
- Preserve the persisted tab state needed to reopen a tab accurately:
  - current path
  - title
  - lock state
  - locked root path
  - tab color
- Exclude transient closes that should not be undoable if they are part of app shutdown/session rehydration flows.

### 2. Command Surface

- Add an `Undo Close Tab` command in `MainViewModel`.
- Wire it into the main UI with at least one accessible entry:
  - File menu or tab context menu
  - optional keyboard shortcut if it does not conflict with existing mappings
- Disable the command when there is no recently closed tab to restore.

### 3. Restore Behavior

- Reopen the most recently closed tab near the original index when practical.
- Restore the tab as an active tab when reopened via undo.
- Preserve locked-tab and color metadata on reopen.
- Ensure undo-close works correctly after:
  - single tab close
  - close-left/right/others
  - duplicate-tab flows

### 4. Persistence Boundaries

- Keep the recently-closed-tab stack session-local unless there is a strong reason to persist it.
- Do not let undo-close interfere with named-session save/load or startup-session restoration semantics.

## Acceptance Criteria

- [x] Closing a tab makes it available to `Undo Close Tab`.
- [x] Executing `Undo Close Tab` reopens the most recently closed tab and reactivates it.
- [x] Reopened tabs preserve lock/color metadata.
- [x] Repeated undo-close actions restore tabs in reverse close order.
- [x] Close-left/right/others contribute tabs to the restore stack in a predictable order.
- [x] Smoke tests cover single close, multi-close, and metadata restoration behavior.
