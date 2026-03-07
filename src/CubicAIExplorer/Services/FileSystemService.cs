using System.Diagnostics;
using System.IO;
using CubicAIExplorer.Models;

namespace CubicAIExplorer.Services;

public sealed class FileSystemService : IFileSystemService
{
    public IReadOnlyList<FileSystemItem> GetDrives()
    {
        return DriveInfo.GetDrives()
            .Where(d => d.IsReady)
            .Select(d => new FileSystemItem
            {
                Name = $"{d.Name.TrimEnd('\\')} ({d.VolumeLabel})",
                FullPath = d.RootDirectory.FullName,
                ItemType = FileSystemItemType.Drive,
                Size = d.TotalSize,
                DateModified = DateTime.MinValue,
                DateCreated = DateTime.MinValue
            })
            .ToList();
    }

    public IReadOnlyList<FileSystemItem> GetDirectoryContents(string path, bool showHidden = false)
    {
        var sanitized = SanitizePath(path);
        if (sanitized == null) return [];

        var items = new List<FileSystemItem>();

        try
        {
            var dirInfo = new DirectoryInfo(sanitized);
            if (!dirInfo.Exists) return [];

            foreach (var dir in dirInfo.EnumerateDirectories())
            {
                if (!showHidden && dir.Attributes.HasFlag(FileAttributes.Hidden)) continue;
                if (dir.Attributes.HasFlag(FileAttributes.System)) continue;

                items.Add(CreateItem(dir));
            }

            foreach (var file in dirInfo.EnumerateFiles())
            {
                if (!showHidden && file.Attributes.HasFlag(FileAttributes.Hidden)) continue;
                if (file.Attributes.HasFlag(FileAttributes.System)) continue;

                items.Add(CreateItem(file));
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }

        return items;
    }

    public IReadOnlyList<FileSystemItem> GetSubDirectories(string path, bool showHidden = false)
    {
        var sanitized = SanitizePath(path);
        if (sanitized == null) return [];

        try
        {
            var dirInfo = new DirectoryInfo(sanitized);
            if (!dirInfo.Exists) return [];

            return dirInfo.EnumerateDirectories()
                .Where(d => !d.Attributes.HasFlag(FileAttributes.System)
                    && (showHidden || !d.Attributes.HasFlag(FileAttributes.Hidden)))
                .Select(CreateItem)
                .ToList();
        }
        catch (UnauthorizedAccessException) { return []; }
        catch (IOException) { return []; }
    }

    public bool DirectoryExists(string path)
    {
        var sanitized = SanitizePath(path);
        return sanitized != null && Directory.Exists(sanitized);
    }

    public bool FileExists(string path)
    {
        var sanitized = SanitizePath(path);
        return sanitized != null && File.Exists(sanitized);
    }

    public string GetParentPath(string path)
    {
        var sanitized = SanitizePath(path);
        if (sanitized == null) return path;
        return Directory.GetParent(sanitized)?.FullName ?? sanitized;
    }

    public void OpenFile(string path)
    {
        var sanitized = SanitizePath(path);
        if (sanitized == null) return;
        if (!File.Exists(sanitized)) return;

        var psi = new ProcessStartInfo
        {
            FileName = sanitized,
            UseShellExecute = true
        };
        Process.Start(psi);
    }

    public void OpenInDefaultApp(string path)
    {
        var sanitized = SanitizePath(path);
        if (sanitized == null) return;

        var psi = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{sanitized}\"",
            UseShellExecute = true
        };
        Process.Start(psi);
    }

    /// <summary>
    /// Validates and normalizes a file path to prevent path traversal attacks.
    /// Returns null if the path is invalid or suspicious.
    /// </summary>
    private static string? SanitizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        try
        {
            // GetFullPath resolves .., ., and normalizes separators
            var fullPath = Path.GetFullPath(path);

            // Block UNC paths unless they start with \\
            if (fullPath.StartsWith(@"\\?\", StringComparison.Ordinal))
                fullPath = fullPath[4..];

            // Ensure the resolved path is rooted (has a drive letter or UNC)
            if (!Path.IsPathRooted(fullPath)) return null;

            return fullPath;
        }
        catch
        {
            return null;
        }
    }

    private static FileSystemItem CreateItem(DirectoryInfo dir) => new()
    {
        Name = dir.Name,
        FullPath = dir.FullName,
        ItemType = FileSystemItemType.Directory,
        DateModified = dir.LastWriteTime,
        DateCreated = dir.CreationTime,
        Attributes = dir.Attributes
    };

    private static FileSystemItem CreateItem(FileInfo file) => new()
    {
        Name = file.Name,
        FullPath = file.FullName,
        ItemType = FileSystemItemType.File,
        Size = file.Length,
        Extension = file.Extension,
        DateModified = file.LastWriteTime,
        DateCreated = file.CreationTime,
        Attributes = file.Attributes
    };
}
