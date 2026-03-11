## Spec 011 - Content Search

- Extended the existing folder-search workflow in [MainWindow.xaml](/C:/dev/CubicAI_rewrite/src/CubicAIExplorer/MainWindow.xaml) instead of adding a separate search surface, which keeps the feature aligned with the current pane-centric UX.
- Kept the implementation in [FileListViewModel.cs](/C:/dev/CubicAI_rewrite/src/CubicAIExplorer/ViewModels/FileListViewModel.cs) because recursive search already lived there and moving it out would have added indirection without improving safety.
- Limited content scanning to a small text-extension allowlist and files at or below 10 MB, then read them in chunks with overlap so long files remain responsive and cross-buffer matches still work.
- Updated [MainViewModel.cs](/C:/dev/CubicAI_rewrite/src/CubicAIExplorer/ViewModels/MainViewModel.cs) and [SavedSearchItem.cs](/C:/dev/CubicAI_rewrite/src/CubicAIExplorer/Models/SavedSearchItem.cs) so saved searches replay both filename and content criteria instead of silently dropping the new filter.
