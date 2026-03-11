# New-file Templates Support

- Added `UserSettings.NewFileTemplatesPath` with a default AppData-backed folder and smoke coverage for settings round-trip behavior.
- Extended `IFileSystemService` with `GetFiles` and `CreateFileFromTemplate` so template enumeration and creation stay behind the filesystem abstraction and path sanitization rules.
- Populated the Edit `New` menu and background pane context-menu `New` submenus dynamically on open so template changes on disk are picked up without restarting the app.
- Reused the existing undo/redo history model by treating template creation like regular file creation, with redo re-copying the original template contents.
- Kept the initial UX lightweight: users can browse to a template folder in Preferences and open that folder directly from the `New` submenu to manage templates without adding another dedicated manager UI.
