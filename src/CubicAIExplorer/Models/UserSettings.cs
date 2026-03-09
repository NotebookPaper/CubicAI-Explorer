namespace CubicAIExplorer.Models;

public sealed class UserSettings
{
    public string DefaultViewMode { get; set; } = "Details";
    public bool ShowHiddenFiles { get; set; }
    public string StartupFolder { get; set; } = string.Empty;
    public bool StartInDualPane { get; set; }
    public bool StartWithPreview { get; set; }
}
