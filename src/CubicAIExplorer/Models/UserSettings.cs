using System.IO;

namespace CubicAIExplorer.Models;

public sealed class UserSettings
{
    public static string GetDefaultNewFileTemplatesPath()
    {
        var overridePath = Environment.GetEnvironmentVariable("CUBICAI_NEWFILE_TEMPLATES_PATH");
        if (!string.IsNullOrWhiteSpace(overridePath))
            return overridePath;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "CubicAIExplorer", "NewFileTemplates");
    }

    public List<DetailsColumnSetting> DetailsColumns { get; set; } = [];
    public string DefaultViewMode { get; set; } = "Details";
    public bool ShowHiddenFiles { get; set; }
    public string StartupFolder { get; set; } = string.Empty;
    public string NewFileTemplatesPath { get; set; } = GetDefaultNewFileTemplatesPath();
    public bool StartInDualPane { get; set; }
    public bool StartWithPreview { get; set; }
    public bool UseShellContextMenu { get; set; } = true;

    // UI Visibility
    public bool ShowToolbar { get; set; } = true;
    public bool ShowAddressBar { get; set; } = true;
    public bool ShowStatusBar { get; set; } = true;
    public bool ShowDrives { get; set; } = true;
    public bool ShowTabs { get; set; } = true;
    public bool ShowRecentFolders { get; set; } = true;
    public bool ShowBookmarks { get; set; } = true;
    public bool ShowSavedSearches { get; set; } = true;
    public NameMatchMode FilterMatchMode { get; set; } = NameMatchMode.Contains;
    public NameMatchMode SearchMatchMode { get; set; } = NameMatchMode.Contains;
    public bool ClearFilterOnFolderChange { get; set; }
    public List<string> FilterHistory { get; set; } = [];

    // Window State
    public double SidebarWidth { get; set; } = 250;
    public double PreviewWidth { get; set; } = 300;
    public List<string> OpenTabs { get; set; } = [];
    public int ActiveTabIndex { get; set; } = 0;
    public string RightPanePath { get; set; } = string.Empty;
    public List<NamedSession> NamedSessions { get; set; } = [];
    public string StartupSessionName { get; set; } = string.Empty;
}
