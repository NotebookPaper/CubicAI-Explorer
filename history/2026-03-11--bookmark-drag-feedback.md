# Bookmark Drag Feedback

- Added transient bookmark drag-feedback state to `MainViewModel` so the UI can describe valid folder, sibling, root, and invalid drops without persisting any bookmark data changes.
- Added bookmark-tree visual cues in `MainWindow.xaml`: inline drag hint text, item highlight styling for the active drop target, and root-surface highlighting for top-level drops.
- Centralized bookmark drop validation through `MainViewModel.CanDropBookmark(...)` so drag-over, drop handling, and smoke coverage all exercise the same logic.
- Extended the smoke harness with a focused bookmark drag-feedback test and wiring assertions for the new bindings and drag-leave cleanup.
- Validation completed with `dotnet build CubicAIExplorer.sln -v minimal`, `dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal`, and `tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe`.
