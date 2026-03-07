namespace CubicAIExplorer.Models;

public sealed class TabItem
{
    public Guid Id { get; } = Guid.NewGuid();
    public required string Path { get; set; }
    public required string Title { get; set; }
}
