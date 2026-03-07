using System.IO;

namespace CubicAIExplorer.Models;

public enum FileSystemItemType
{
    Drive,
    Directory,
    File
}

public sealed class FileSystemItem
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public FileSystemItemType ItemType { get; init; }
    public long Size { get; init; }
    public DateTime DateModified { get; init; }
    public DateTime DateCreated { get; init; }
    public string Extension { get; init; } = string.Empty;
    public FileAttributes Attributes { get; init; }
    public bool IsHidden => Attributes.HasFlag(FileAttributes.Hidden);
    public bool IsSystem => Attributes.HasFlag(FileAttributes.System);
    public bool IsReadOnly => Attributes.HasFlag(FileAttributes.ReadOnly);

    public string DisplaySize => ItemType == FileSystemItemType.File
        ? FormatSize(Size)
        : string.Empty;

    public string TypeDescription => ItemType switch
    {
        FileSystemItemType.Drive => "Drive",
        FileSystemItemType.Directory => "File folder",
        FileSystemItemType.File => string.IsNullOrEmpty(Extension)
            ? "File"
            : $"{Extension.TrimStart('.').ToUpperInvariant()} File",
        _ => "Unknown"
    };

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}
