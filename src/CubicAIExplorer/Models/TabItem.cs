namespace CubicAIExplorer.Models;

public sealed class TabItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Path { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public string LockedRootPath { get; set; } = string.Empty;
    public string TabColor { get; set; } = string.Empty;
}
