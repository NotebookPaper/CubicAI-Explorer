# 008 - App Icon Refresh (v2)

## Status

COMPLETE

## Summary

Update the application's visual identity by swapping the legacy icon for the new multi-size `appicon-v2.ico` asset.

## Problem

The current application icon is either a placeholder or a legacy asset. A new set of high-quality assets (SVG, ICO, and PNGs) has been prepared in the `Resources/` directory, but the project is not yet configured to use them as the primary application identity.

## Proposed Changes

### 1. Project Configuration
- Update `src\CubicAIExplorer\CubicAIExplorer.csproj` to set `<ApplicationIcon>Resources\appicon-v2.ico</ApplicationIcon>`.
- This ensures the compiled `.exe` shows the correct icon in Windows Explorer.

### 2. Window Icon Binding
- Update `MainWindow.xaml` to set the `Icon` property to `Resources/appicon-v2.ico` (or `appicon-v2-32.png` for the taskbar/titlebar if necessary).
- Verify that the icon is correctly embedded as a Resource and resolved at runtime.

### 3. Verification
- Perform a clean build and verify the icon in:
  - Taskbar (small)
  - Title bar (small)
  - Windows Explorer (Extra Large, Medium, and List views)
  - Task Manager

## Acceptance Criteria

- [x] The `CubicAIExplorer.exe` file is configured to use the new v2 icon in project output metadata.
- [x] The application window uses the new v2 icon in the title bar and taskbar binding.
- [x] The icon asset contains the standard 16x16, 32x32, 48x48, and 256x256 sizes.
- [x] No build warnings regarding missing resource assets.
