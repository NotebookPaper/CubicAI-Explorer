# Empty Recycle Bin

- Added a dedicated `Tools > Empty Recycle Bin...` action so the shell-maintenance workflow is available without leaving CubicAI Explorer.
- Kept the destructive confirmation in `MainWindow.xaml.cs` and the actual operation in `MainViewModel`/`IFileSystemService` so the command path stays smoke-testable.
- Implemented recycle-bin emptying through `SHEmptyRecycleBinW` with progress UI and sound suppressed to avoid duplicate prompts and keep behavior predictable after confirmation.
- Extended the smoke harness with a recording filesystem assertion that verifies service invocation and success status text.
- Validation completed with `dotnet build CubicAIExplorer.sln -v minimal`, `dotnet build tests/CubicAIExplorer.SmokeTests\CubicAIExplorer.SmokeTests.csproj -v minimal`, and `tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe`.
