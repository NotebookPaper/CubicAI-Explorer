# 022 - WPF Interaction Smoke Tests
<!-- NR_OF_TRIES: 1 -->

## Status: COMPLETE

## Summary

Expand the smoke harness to cover real WPF interaction behaviors that are currently missed by viewmodel-only and XAML-wiring tests, with bookmark drag/drop as the primary target.

## Problem

The current smoke suite is broad, but it mostly validates:

- viewmodel state transitions
- persistence and service behavior
- command routing
- XAML loading and wiring presence

Recent bookmark drag regressions showed that this still leaves a gap: actual WPF hit-testing, mouse capture, and tree/list interaction behavior can break while the existing smoke tests continue to pass.

## Proposed Changes

### 1. Dispatcher-Based Window Interaction Helpers

- Add smoke-test helpers that create `MainWindow` on the WPF dispatcher and interact with real controls.
- Keep these tests narrowly scoped and deterministic to avoid turning the smoke suite into a flaky UI automation layer.
- Reuse existing headless-safe dialog and service patterns where possible.

### 2. Bookmark Drag/Drop Interaction Coverage

- Add interaction-level coverage for bookmark tree drag scenarios:
  - drag over row center resolves `into`
  - drag over row edge resolves `after`
  - drag over empty tree space resolves root drop
  - collapsed folder hover can expand during drag
- Verify target containers remain hit-testable during internal drag with mouse capture.

### 3. High-Value Additional UI Interaction Cases

- Add at least one interaction test for each of these risk areas:
  - bookmarks bar drop target highlighting/drop acceptance
  - file-list or folder-tree drag destination routing
  - tab-strip interactions that rely on actual WPF layout/containers

### 4. Test Discipline

- Keep the new interaction tests small in count and high in signal.
- Run them late in the smoke suite if they require more WPF application state.
- Avoid modal UI and timing-heavy assertions; prefer dispatcher pumping with explicit state checks.

## Implemented

- Added dispatcher-based smoke helpers that create and exercise a real hidden `MainWindow` instance with deterministic dispatcher pumping.
- Added bookmark-tree interaction smoke coverage for real `TreeViewItem` hit-testing, including `into`, `after`, and root targeting plus hover-expand behavior and mouse-capture resilience.
- Added a tab-strip interaction smoke test that validates real overflow layout, overflow-menu population, and tab activation through the WPF menu surface.

## Acceptance Criteria

- [x] The smoke suite includes dispatcher-based `MainWindow` interaction helpers.
- [x] At least one smoke test reproduces and guards the bookmark-tree drag hit-testing path.
- [x] Bookmark drag smoke coverage verifies `into`, `after`, and root targeting with real WPF containers.
- [x] At least one additional interaction smoke test covers a non-bookmark WPF interaction path.
- [x] The expanded smoke suite remains headless-safe and deterministic in the local harness.
