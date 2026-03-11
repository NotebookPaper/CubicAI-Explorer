# Shell Verb Execution

- Added a shared `ExecuteShellVerb` method on `IFileSystemService` so alternate launches stay inside the existing shell-aware service boundary.
- Implemented shell-verb execution with `ShellExecuteEx`, explicit Win32 error propagation, and sanitized path validation before invocation.
- Exposed `Open in New Window` and `Run as Administrator` from `MainViewModel`, with menu/context-menu wiring in `MainWindow`.
- Kept the command path headless-testable by verifying requested verbs through the smoke harness recording filesystem double instead of launching real elevated UI.
- Validation completed with `dotnet build CubicAIExplorer.sln -v minimal`, `dotnet build tests/CubicAIExplorer.SmokeTests\CubicAIExplorer.SmokeTests.csproj -v minimal`, and `tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe`.
