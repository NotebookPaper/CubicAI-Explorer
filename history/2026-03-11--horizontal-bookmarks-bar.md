## Spec 012 - Horizontal Bookmarks Bar

- Added the bookmarks bar in [MainWindow.xaml](/C:/dev/CubicAI_rewrite/src/CubicAIExplorer/MainWindow.xaml) as a separate surface below the address area instead of folding it into the sidebar, which keeps the original bookmark tree intact while matching the spec's one-click toolbar behavior.
- Reused the existing bookmark commands in [MainWindow.xaml.cs](/C:/dev/CubicAI_rewrite/src/CubicAIExplorer/MainWindow.xaml.cs) for click and context-menu actions so the bar does not introduce a second bookmark-management path.
- Extended [MainViewModel.cs](/C:/dev/CubicAI_rewrite/src/CubicAIExplorer/ViewModels/MainViewModel.cs) and [UserSettings.cs](/C:/dev/CubicAI_rewrite/src/CubicAIExplorer/Models/UserSettings.cs) with a dedicated `ShowBookmarksBar` setting because the existing `ShowBookmarks` flag controls the sidebar tree and could not safely be repurposed.
- Hardened `AddBookmarkFromPath` to reject non-directories and top-level duplicates so file-list drops onto the bar persist cleanly without creating repeated bookmark entries.
