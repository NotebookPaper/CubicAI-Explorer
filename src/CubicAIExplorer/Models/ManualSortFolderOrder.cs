namespace CubicAIExplorer.Models;

public sealed class ManualSortFolderOrder
{
    public string FolderPath { get; set; } = string.Empty;
    public List<string> OrderedNames { get; set; } = [];
}
