# Windows Shell Context Menu Integration

- Date: 2026-03-11
- Scope: UI interop and native shell integration

Implemented the REAL Windows shell context menu for `FileListView` and `RightPaneListView`. This allows users to access all shell extensions, third-party context menu items (like Git, 7-Zip, etc.), and native Windows file operations directly from CubicAI Explorer.

Key changes:
- Added `UseShellContextMenu` setting to `UserSettings`.
- Added a toggle for the shell context menu in the Preferences window.
- Implemented `ShellContextMenuHelper` using `IShellFolder` and `IContextMenu` COM interfaces to host the native menu.
- Integrated the shell menu into `MainWindow.xaml.cs`, intercepting `ContextMenuOpening` events when the setting is enabled.
- Provided a fallback to the internal WPF context menu if the native menu fails or is disabled.

This significantly improves the "power user" workflow by providing full access to the Windows shell ecosystem.
