## Spec 010 - Breadcrumb Dropdown Navigation

- Added a small dropdown button beside each non-terminal breadcrumb segment in [MainWindow.xaml](/C:/dev/CubicAI_rewrite/src/CubicAIExplorer/MainWindow.xaml) so users can branch into sibling folders without switching to text-path edit mode.
- Kept the loading logic in [MainViewModel.cs](/C:/dev/CubicAI_rewrite/src/CubicAIExplorer/ViewModels/MainViewModel.cs) and reused `IFileSystemService.GetSubDirectories`, which keeps path sanitization and filesystem access inside the existing service boundary.
- Used placeholder menu entries for `Loading...` and `No subfolders` so the context menu can open immediately while the directory listing is fetched on a background thread.
- Routed selection through the existing current-pane navigation path instead of special-casing breadcrumb jumps, which automatically preserves tab back/forward stacks and keeps the feature low-risk.
