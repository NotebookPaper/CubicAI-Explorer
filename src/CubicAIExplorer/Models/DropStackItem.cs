namespace CubicAIExplorer.Models;

public sealed class DropStackItem
{
    public required string FullPath { get; init; }
    public required string Name { get; init; }
    public required string ParentPath { get; init; }
    public bool IsDirectory { get; init; }
    public string ItemTypeText => IsDirectory ? "Folder" : "File";
}
