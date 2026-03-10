Implemented persistent details-column customization for the details view.

- Added `DetailsColumnId` and `DetailsColumnSetting` models so width, visibility, and order persist through the existing settings file.
- Main window now builds the details `GridView` from saved settings instead of a fixed column definition.
- Added View menu commands for show/hide, move left/right, auto-size, and reset so column order is user-configurable without adding a new dialog.
- Persisted the live details layout before mode switches and on window close so width/order changes survive restart.
- Expanded smoke coverage for default layout behavior, normalized saves, settings round-trip persistence, and new XAML wiring.
