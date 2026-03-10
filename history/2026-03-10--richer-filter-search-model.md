Implemented spec 002 richer filter/search model.

- Added shared `Contains`, `Wildcard`, and `Exact` name-match modes for inline filters and recursive search.
- Saved searches now persist their search match mode so replayed searches keep the same semantics.
- Persisted filter history through `UserSettings` and exposed it in the main window for quick reuse.
- Added an optional clear-on-navigation filter behavior and propagated the filter/search preferences to new tabs and both panes.
- Expanded smoke coverage for filter modes, search modes, saved-search mode persistence, settings round-trip, and XAML wiring.
