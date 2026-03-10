namespace CubicAIExplorer.Models;

public sealed class SavedSearchItem
{
    public Guid Id { get; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string SearchTerm { get; set; }
    public required string SearchPath { get; set; }
}
