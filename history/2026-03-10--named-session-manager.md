Implemented spec 001 named session manager.

- Added a persisted `NamedSession` model inside `UserSettings` so named workspaces live in the existing settings flow rather than a separate store.
- Main window now exposes a Sessions submenu for save-as, update, load, delete, and startup-session selection.
- Startup restoration now prefers a configured named session and otherwise preserves the prior auto-restore behavior.
- Smoke coverage was expanded for session save/load/delete/startup behavior.
- Hardened the bookmark file watcher to run safely in the headless smoke harness when no WPF `Application` instance exists.
