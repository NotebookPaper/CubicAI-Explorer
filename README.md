# CubicAI Explorer

CubicAI Explorer is a modern Windows file manager written in C#/WPF. It is a rewrite of the abandoned Delphi-based CubicExplorer project, with a focus on a native desktop UI, practical file-management workflows, and a conservative dependency surface.

The "AI" in the name refers to AI-assisted development of the app, not AI features inside the file manager.

## Status

This repository is actively being developed and already has a working desktop application with broad smoke-test coverage.

Current implemented features include:

- tabbed browsing and dual-pane mode
- lazy-loaded folder tree and sortable file list
- copy, move, paste, delete, permanent delete, rename, and new-folder workflows
- undo/redo history for core file operations
- Windows Explorer clipboard interop and drag/drop
- bookmarks, recent folders, breadcrumbs, and address autocomplete
- search/filter support and saved searches
- preview panel with text, image, folder, archive, and metadata fallback states
- ZIP archive browsing and extraction
- background file-operation queue with status, progress, and cancel support
- persisted settings and window placement

## Screenshots

Screenshots can be added here once the UI is ready to present publicly.

## Requirements

- Windows
- .NET 8 SDK

This project targets `net8.0-windows` and uses WPF, so it is intentionally Windows-only.

## Build

```bash
dotnet build CubicAIExplorer.sln -v minimal
```

## Run

```bash
dotnet run --project src/CubicAIExplorer/CubicAIExplorer.csproj
```

## Smoke Tests

```bash
dotnet run --project tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj
```

The smoke suite covers core file operations, queue behavior, preview/archive behavior, saved searches, settings, and major XAML wiring checks.

## Project Structure

```text
CubicAIExplorer.sln
src/CubicAIExplorer/
  App.xaml(.cs)              Application entry and service wiring
  MainWindow.xaml(.cs)       Main shell UI
  Models/                    App models and records
  Services/                  File system, queue, clipboard, settings, navigation
  ViewModels/                MVVM view models
  Views/                     Dialogs and supporting windows
tests/CubicAIExplorer.SmokeTests/
  Program.cs                 Smoke-test harness
```

## Tech Stack

- .NET 8
- WPF
- CommunityToolkit.Mvvm

No additional NuGet packages are used beyond the MVVM toolkit.

## Design Notes

- MVVM with constructor-injected services
- file paths are sanitized through `FileSystemService`
- file operations prefer async/background execution where practical
- no WinForms dependency is added for folder-picking or shell UI shortcuts

## License

This project is licensed under MPL 1.1. See [LICENSE](LICENSE).
