# Queue History Error Reporting

- Added a bounded recent-operation history model to `FileOperationQueueService` so the queue retains more than a single last-status string.
- Recorded explicit succeeded, failed, and canceled states along with retained detail text; failures flatten nested exception messages into a readable multi-line detail block.
- Reused the existing status-bar queue-details toggle in `MainWindow.xaml` by anchoring a compact popup to the button instead of adding a larger surface or modal dialog.
- Kept the history strictly in-memory and capped so the queue remains lightweight and the popup stays legible during long sessions.
- Added smoke coverage for failed queue history entries and XAML wiring checks to prevent regressions in the popup bindings.
- Validation completed with `dotnet build CubicAIExplorer.sln -v minimal`, `dotnet build tests/CubicAIExplorer.SmokeTests\CubicAIExplorer.SmokeTests.csproj -v minimal`, and `tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe`.
