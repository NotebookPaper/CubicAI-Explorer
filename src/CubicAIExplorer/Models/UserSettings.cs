namespace CubicAIExplorer.Models;

public sealed class UserSettings
{
    public string DefaultViewMode { get; set; } = "Details";
    public bool ShowHiddenFiles { get; set; }
    public string StartupFolder { get; set; } = string.Empty;
    public bool StartInDualPane { get; set; }
    public bool StartWithPreview { get; set; }

    // UI Visibility
    public bool ShowToolbar { get; set; } = true;
    public bool ShowAddressBar { get; set; } = true;
    public bool ShowStatusBar { get; set; } = true;
    public bool ShowDrives { get; set; } = true;
    public bool ShowTabs { get; set; } = true;
    public bool ShowRecentFolders { get; set; } = true;
    public bool ShowBookmarks { get; set; } = true;
    public bool ShowSavedSearches { get; set; } = true;
}
