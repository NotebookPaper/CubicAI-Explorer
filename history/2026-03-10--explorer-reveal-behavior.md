## Explorer Reveal Behavior

- Moved `Open in Explorer` shell launching behind `IFileSystemService` so the behavior is testable and no longer lives directly in `MainWindow.xaml.cs`.
- Added a dedicated reveal path that uses Explorer selection for a single selected file or folder.
- Kept folder-open fallback behavior for no-selection and multi-selection cases to avoid ambiguous or partial highlighting.
- Added smoke coverage at the `MainViewModel` level to verify the selected item path is sent to the shell reveal call.
