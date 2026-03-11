using System.IO;

namespace CubicAIExplorer.Models;

public enum FileSystemItemType
{
    Drive,
    Directory,
    File,
    Bookmark
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
    public string ShellTypeName { get; init; } = string.Empty;
    public FileAttributes Attributes { get; init; }
    public bool IsHidden => Attributes.HasFlag(FileAttributes.Hidden);
    public bool IsSystem => Attributes.HasFlag(FileAttributes.System);
    public bool IsReadOnly => Attributes.HasFlag(FileAttributes.ReadOnly);

    public ShellProperties ShellProperties { get; init; } = ShellProperties.Empty;

    public string DisplaySize => ItemType == FileSystemItemType.File
        ? FormatSize(Size)
        : string.Empty;

    public string TypeDescription => !string.IsNullOrWhiteSpace(ShellTypeName)
        ? ShellTypeName
        : ItemType switch
    {
        FileSystemItemType.Drive => "Drive",
        FileSystemItemType.Directory => "File folder",
        FileSystemItemType.File => string.IsNullOrEmpty(Extension)
            ? "File"
            : $"{Extension.TrimStart('.').ToUpperInvariant()} File",
        _ => "Unknown"
    };

    public string GroupNameLabel => GetNameGroupLabel(Name);
    public string GroupTypeLabel => GetTypeGroupLabel();
    public string GroupSizeLabel => GetSizeGroupLabel();
    public string GroupDateModifiedLabel => GetDateModifiedGroupLabel(DateModified);

    public int GroupNameRank => GetNameGroupLabel(Name)[0];
    public int GroupTypeRank => ItemType switch
    {
        FileSystemItemType.Drive => 0,
        FileSystemItemType.Directory => 1,
        _ => 2
    };

    public int GroupSizeRank => ItemType switch
    {
        FileSystemItemType.Drive => -2,
        FileSystemItemType.Directory => -1,
        _ => GetSizeRank(Size)
    };

    public int GroupDateModifiedRank => GetDateModifiedGroupRank(DateModified);

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };

    private static string GetNameGroupLabel(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "#";

        var first = char.ToUpperInvariant(name[0]);
        return char.IsLetter(first) ? first.ToString() : "#";
    }

    private string GetTypeGroupLabel() => ItemType switch
    {
        FileSystemItemType.Drive => "Drives",
        FileSystemItemType.Directory => "Folders",
        _ => TypeDescription
    };

    private string GetSizeGroupLabel()
    {
        return ItemType switch
        {
            FileSystemItemType.Drive => "Drives",
            FileSystemItemType.Directory => "Folders",
            _ => GetSizeRank(Size) switch
            {
                0 => "Empty",
                1 => "Tiny (< 100 KB)",
                2 => "Small (100 KB - 1 MB)",
                3 => "Medium (1 MB - 16 MB)",
                4 => "Large (16 MB - 128 MB)",
                _ => "Huge (128 MB+)"
            }
        };
    }

    private static int GetSizeRank(long size)
    {
        if (size <= 0)
            return 0;
        if (size < 100 * 1024)
            return 1;
        if (size < 1024 * 1024)
            return 2;
        if (size < 16L * 1024 * 1024)
            return 3;
        if (size < 128L * 1024 * 1024)
            return 4;
        return 5;
    }

    private static string GetDateModifiedGroupLabel(DateTime value)
    {
        return GetDateModifiedGroupRank(value) switch
        {
            0 => "Today",
            1 => "Yesterday",
            2 => "Earlier this week",
            3 => "Last week",
            4 => "Earlier this month",
            5 => "Earlier this year",
            _ => "Older"
        };
    }

    private static int GetDateModifiedGroupRank(DateTime value)
    {
        var today = DateTime.Today;
        var date = value.Date;
        var deltaDays = (today - date).TotalDays;
        if (deltaDays <= 0)
            return 0;
        if (deltaDays <= 1)
            return 1;

        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        if (date >= startOfWeek)
            return 2;
        if (date >= startOfWeek.AddDays(-7))
            return 3;
        if (date.Month == today.Month && date.Year == today.Year)
            return 4;
        if (date.Year == today.Year)
            return 5;
        return 6;
    }
}
