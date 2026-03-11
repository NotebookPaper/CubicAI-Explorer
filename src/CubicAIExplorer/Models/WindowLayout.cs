namespace CubicAIExplorer.Models;

public sealed class WindowLayout
{
    public string Name { get; set; } = string.Empty;
    public double SidebarWidth { get; set; } = 250;
    public double PreviewWidth { get; set; } = 300;
    public bool IsDualPaneMode { get; set; }
    public bool IsPreviewVisible { get; set; }
    public bool ShowDrives { get; set; } = true;
    public bool ShowRecentFolders { get; set; } = true;
    public bool ShowBookmarks { get; set; } = true;
    public bool ShowBookmarksBar { get; set; } = true;
    public bool ShowSavedSearches { get; set; } = true;
    public string ViewMode { get; set; } = "Details";
}
