namespace CubicAIExplorer.Models;

public sealed class BookmarkItem
{
    public Guid Id { get; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Path { get; set; }
    public bool IsFolder { get; set; }
    public List<BookmarkItem> Children { get; set; } = [];
}
