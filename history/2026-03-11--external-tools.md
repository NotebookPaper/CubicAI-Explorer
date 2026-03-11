Implemented spec 018 by keeping external tool definitions inside the existing `UserSettings` flow instead of introducing a separate config file or service.

Key decisions:
- External tools are normalized and persisted as simple `Name` / `ToolPath` / `Arguments` records, which keeps settings round-trips backward compatible and easy to inspect.
- Process launching stays inside `FileSystemService` via a dedicated `LaunchExternalTool` method so the view model still routes filesystem-adjacent behavior through the existing abstraction boundary.
- Argument expansion only supports `%p` for the selected file path, and when `%p` is omitted the selected file path is appended automatically so simple tool definitions still work.

Lessons:
- WPF smoke tests that instantiate real windows should stay near the end of the smoke run; creating them too early can perturb later async/UI-sensitive tests.
- Context-menu dynamic submenu population is simpler to maintain in the window layer than trying to express mixed static/dynamic menu trees purely in XAML bindings.
