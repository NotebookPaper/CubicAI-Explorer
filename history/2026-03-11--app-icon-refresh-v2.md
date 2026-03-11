# App Icon Refresh (v2)

- Updated the project output icon metadata to use `Resources\appicon-v2.ico` so the built executable carries the refreshed icon asset.
- Updated `MainWindow.xaml` to use the same v2 ICO resource for title-bar and taskbar identity instead of the legacy PNG binding.
- Extended the smoke harness with an icon-configuration test that verifies the project wiring and the presence of 16x16, 32x32, 48x48, and 256x256 icon frames in the ICO.
- Validation completed with `dotnet build CubicAIExplorer.sln -v minimal`, `dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal`, and `tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe`.

