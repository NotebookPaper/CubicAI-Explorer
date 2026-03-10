# 2026-03-10 - Crowded Tab Overflow Affordances

- Added a scrollable tab-header row in `MainWindow.xaml` so crowded sessions no longer collapse tab headers into unusable widths.
- Added left/right tab-strip scroll buttons and a `More Tabs` dropdown populated from the live tab collection in `MainWindow.xaml.cs`.
- Kept the implementation in the existing window layer rather than pushing overflow state into `MainViewModel`; this keeps the behavior UI-specific and avoids complicating tab persistence.
- Active-tab changes, tab-count changes, and window-size changes now queue a tab-strip refresh so the selected tab is scrolled back into view after overflow.
- Smoke coverage stays lightweight and source-driven for this slice because the behavior is mostly template/code-behind wiring rather than domain logic.
