# 005 - Shell Property Exposure

## Status: COMPLETE

## Summary

Expose richer shell-backed metadata (such as Company, Copyright, Version, Dimensions, and Duration) in the Details view and Properties dialog by integrating with the Windows Property System (IPropertyStore).

## Problem

CubicAI Explorer currently shows basic filesystem metadata (Size, Date Modified, Type). While functional, it lacks the "deep" metadata that Windows Explorer provides, such as:

- Version and Company info for executables (.exe, .dll)
- Dimensions for images (.png, .jpg, etc.)
- Duration for media files (.mp3, .mp4, etc.)
- Author, Title, and other document properties

Power users rely on these columns for sorting and identifying files quickly without opening them or showing the preview panel.

## Goals

- Implement a robust retrieval mechanism for shell properties using `IShellItem2` and `IPropertyStore`.
- Expose these properties as optional columns in the Details view.
- Show these properties in the internal Properties dialog.
- Keep the implementation efficient (lazy loading where possible).

## Non-Goals

- Editing shell properties in this spec (read-only first).
- Implementing content-based metadata extraction for types not supported by the Windows Shell.
- Redesigning the column-customization UI (use existing View menu toggles).

## Functional Requirements

1. Add `ShellPropertyService` (or similar helper) to retrieve metadata via `IPropertyStore`.
2. Support at least these property keys:
   - `PKEY_Company`
   - `PKEY_Copyright`
   - `PKEY_FileDescription` (Product Name)
   - `PKEY_FileVersion`
   - `PKEY_Image_Dimensions`
   - `PKEY_Media_Duration`
3. Update `FileSystemItem` to store a dictionary or specific fields for these properties.
4. Update `FileSystemService` to populate these properties.
5. Update `FileListViewModel` and `MainWindow` to support toggling these new columns in Details view.
6. Columns should be: Company, Version, Dimensions, Duration.
7. Update `PropertiesDialog` to display these extra properties in a "Details" section or below the standard attributes.

## Implementation Constraints

- Use P/Invoke for `IShellItem2` and `IPropertyStore`.
- No new NuGet packages.
- Follow existing MVVM patterns.
- Ensure no significant performance regression when loading large directories (lazily fetch properties if needed).

## Acceptance Criteria

1. Users can enable "Company", "Version", "Dimensions", and "Duration" columns from the View > Columns menu.
2. The columns display correct values from the Windows Shell for relevant file types.
3. The internal Properties dialog (Alt+Enter) shows these extra details.
4. Sorting by these new columns works as expected.
5. Column visibility and width are persisted in settings.
6. `dotnet build CubicAIExplorer.sln -v minimal` passes.
7. Smoke tests pass (add basic verification for property presence in properties dialog).

## Verification Commands

```bash
dotnet build CubicAIExplorer.sln -v minimal
dotnet build tests/CubicAIExplorer.SmokeTests/CubicAIExplorer.SmokeTests.csproj -v minimal
tests\CubicAIExplorer.SmokeTests\bin\Debug\net8.0-windows\CubicAIExplorer.SmokeTests.exe
```

<!-- NR_OF_TRIES: 1 -->
