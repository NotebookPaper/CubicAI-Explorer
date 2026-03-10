namespace CubicAIExplorer.Models;

public sealed class NamedSession
{
    public string Name { get; set; } = string.Empty;
    public List<string> OpenTabs { get; set; } = [];
    public int ActiveTabIndex { get; set; }
    public string RightPanePath { get; set; } = string.Empty;
    public bool IsDualPaneMode { get; set; }
}
