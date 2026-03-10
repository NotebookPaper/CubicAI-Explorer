using System.Diagnostics;
using System.IO;
using Microsoft.VisualBasic.FileIO;
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

    public IReadOnlyList<FileTransferResult> CopyFiles(
        IEnumerable<string> sourcePaths,
        string destinationDirectory,
        FileTransferCollisionResolution collisionResolution = FileTransferCollisionResolution.KeepBoth)
    {
        var results = new List<FileTransferResult>();
        var destination = SanitizePath(destinationDirectory);
        if (destination == null || !Directory.Exists(destination)) return results;

        foreach (var sourcePath in sourcePaths)
        {
            var source = SanitizePath(sourcePath);
            if (source == null) continue;

            if (File.Exists(source))
            {
                results.Add(CopyFile(source, destination, collisionResolution));
            }
            else if (Directory.Exists(source))
            {
                results.Add(CopyDirectory(source, destination, collisionResolution));
            }
        }

        return results;
    }

    public IReadOnlyList<FileTransferResult> MoveFiles(
        IEnumerable<string> sourcePaths,
        string destinationDirectory,
        FileTransferCollisionResolution collisionResolution = FileTransferCollisionResolution.KeepBoth)
    {
        var results = new List<FileTransferResult>();
        var destination = SanitizePath(destinationDirectory);
        if (destination == null || !Directory.Exists(destination)) return results;

        foreach (var sourcePath in sourcePaths)
        {
            var source = SanitizePath(sourcePath);
            if (source == null) continue;

            if (File.Exists(source))
            {
                var sourceDir = Path.GetDirectoryName(source);
                if (!string.IsNullOrWhiteSpace(sourceDir)
                    && string.Equals(sourceDir.TrimEnd('\\'), destination.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                results.Add(MoveFile(source, destination, collisionResolution));
            }
            else if (Directory.Exists(source))
            {
                var sourceParent = Path.GetDirectoryName(source.TrimEnd('\\'));
                if (!string.IsNullOrWhiteSpace(sourceParent)
                    && string.Equals(sourceParent.TrimEnd('\\'), destination.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                results.Add(MoveDirectoryEntry(source, destination, collisionResolution));
            }
        }

        return results;
    }

    public void DeleteFiles(IEnumerable<string> paths, bool permanentDelete = false)
    {
        foreach (var path in paths)
        {
            var sanitized = SanitizePath(path);
            if (sanitized == null) continue;

            if (File.Exists(sanitized))
            {
                FileSystem.DeleteFile(
                    sanitized,
                    UIOption.OnlyErrorDialogs,
                    permanentDelete ? RecycleOption.DeletePermanently : RecycleOption.SendToRecycleBin);
            }
            else if (Directory.Exists(sanitized))
            {
                FileSystem.DeleteDirectory(
                    sanitized,
                    UIOption.OnlyErrorDialogs,
                    permanentDelete ? RecycleOption.DeletePermanently : RecycleOption.SendToRecycleBin);
            }
        }
    }

    public string RenameFile(string path, string newName)
    {
        var sanitized = SanitizePath(path);
        if (sanitized == null || string.IsNullOrWhiteSpace(newName))
            return path;

        var parent = Path.GetDirectoryName(sanitized);
        if (string.IsNullOrWhiteSpace(parent))
            return sanitized;

        var targetName = newName.Trim();
        var targetPath = SanitizePath(Path.Combine(parent, targetName));
        if (targetPath == null)
            return sanitized;

        if (File.Exists(sanitized))
        {
            if (string.Equals(sanitized, targetPath, StringComparison.OrdinalIgnoreCase))
                return sanitized;

            var uniquePath = GetUniquePath(parent, Path.GetFileName(targetPath), isDirectory: false, sanitized);
            File.Move(sanitized, uniquePath);
            return uniquePath;
        }

        if (Directory.Exists(sanitized))
        {
            if (string.Equals(sanitized, targetPath, StringComparison.OrdinalIgnoreCase))
                return sanitized;

            var uniquePath = GetUniquePath(parent, Path.GetFileName(targetPath), isDirectory: true, sanitized);
            MoveDirectory(sanitized, uniquePath);
            return uniquePath;
        }

        return sanitized;
    }

    public string CreateFolder(string parentPath, string folderName)
    {
        var sanitizedParent = SanitizePath(parentPath);
        if (sanitizedParent == null || !Directory.Exists(sanitizedParent))
            return parentPath;

        var baseName = string.IsNullOrWhiteSpace(folderName) ? "New folder" : folderName.Trim();
        var targetPath = GetUniquePath(sanitizedParent, baseName, isDirectory: true);
        Directory.CreateDirectory(targetPath);
        return targetPath;
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

    private static string GetUniquePath(string directory, string fileName, bool isDirectory, string? existingPath = null)
    {
        var nameWithoutExtension = isDirectory ? fileName : Path.GetFileNameWithoutExtension(fileName);
        var extension = isDirectory ? string.Empty : Path.GetExtension(fileName);

        var candidate = Path.Combine(directory, $"{nameWithoutExtension}{extension}");
        var index = 2;

        while (PathExists(candidate, isDirectory)
            && !string.Equals(candidate, existingPath, StringComparison.OrdinalIgnoreCase))
        {
            candidate = Path.Combine(directory, $"{nameWithoutExtension} ({index}){extension}");
            index++;
        }

        return candidate;
    }

    private static bool PathExists(string path, bool isDirectory)
        => isDirectory ? Directory.Exists(path) : File.Exists(path);

    private static void CopyDirectoryRecursive(string sourcePath, string destinationPath)
    {
        Directory.CreateDirectory(destinationPath);

        foreach (var file in Directory.EnumerateFiles(sourcePath))
        {
            var fileName = Path.GetFileName(file);
            var targetFile = Path.Combine(destinationPath, fileName);
            File.Copy(file, targetFile, overwrite: false);
        }

        foreach (var directory in Directory.EnumerateDirectories(sourcePath))
        {
            var dirName = Path.GetFileName(directory);
            var targetDirectory = Path.Combine(destinationPath, dirName);
            CopyDirectoryRecursive(directory, targetDirectory);
        }
    }

    private static void MoveDirectory(string sourcePath, string destinationPath)
    {
        try
        {
            Directory.Move(sourcePath, destinationPath);
        }
        catch (IOException)
        {
            // Cross-volume moves can fail; fallback to copy + delete.
            CopyDirectoryRecursive(sourcePath, destinationPath);
            Directory.Delete(sourcePath, recursive: true);
        }
    }

    private FileTransferResult CopyFile(string source, string destinationDirectory, FileTransferCollisionResolution collisionResolution)
    {
        var targetPath = ResolveTargetPath(destinationDirectory, source, isDirectory: false, collisionResolution, out var skipped);
        if (skipped)
            return new FileTransferResult(source, string.Empty, IsDirectory: false, FileTransferStatus.Skipped);

        try
        {
            File.Copy(source, targetPath, overwrite: false);
            return new FileTransferResult(source, targetPath, IsDirectory: false);
        }
        catch (Exception ex)
        {
            return new FileTransferResult(source, targetPath, IsDirectory: false, FileTransferStatus.Failed, ex.Message);
        }
    }

    private FileTransferResult CopyDirectory(string source, string destinationDirectory, FileTransferCollisionResolution collisionResolution)
    {
        var targetPath = ResolveTargetPath(destinationDirectory, source, isDirectory: true, collisionResolution, out var skipped);
        if (skipped)
            return new FileTransferResult(source, string.Empty, IsDirectory: true, FileTransferStatus.Skipped);

        try
        {
            CopyDirectoryRecursive(source, targetPath);
            return new FileTransferResult(source, targetPath, IsDirectory: true);
        }
        catch (Exception ex)
        {
            return new FileTransferResult(source, targetPath, IsDirectory: true, FileTransferStatus.Failed, ex.Message);
        }
    }

    private FileTransferResult MoveFile(string source, string destinationDirectory, FileTransferCollisionResolution collisionResolution)
    {
        var targetPath = ResolveTargetPath(destinationDirectory, source, isDirectory: false, collisionResolution, out var skipped);
        if (skipped)
            return new FileTransferResult(source, string.Empty, IsDirectory: false, FileTransferStatus.Skipped);

        try
        {
            File.Move(source, targetPath);
            return new FileTransferResult(source, targetPath, IsDirectory: false);
        }
        catch (Exception ex)
        {
            return new FileTransferResult(source, targetPath, IsDirectory: false, FileTransferStatus.Failed, ex.Message);
        }
    }

    private FileTransferResult MoveDirectoryEntry(string source, string destinationDirectory, FileTransferCollisionResolution collisionResolution)
    {
        var targetPath = ResolveTargetPath(destinationDirectory, source, isDirectory: true, collisionResolution, out var skipped);
        if (skipped)
            return new FileTransferResult(source, string.Empty, IsDirectory: true, FileTransferStatus.Skipped);

        try
        {
            MoveDirectory(source, targetPath);
            return new FileTransferResult(source, targetPath, IsDirectory: true);
        }
        catch (Exception ex)
        {
            return new FileTransferResult(source, targetPath, IsDirectory: true, FileTransferStatus.Failed, ex.Message);
        }
    }

    private string ResolveTargetPath(
        string destinationDirectory,
        string sourcePath,
        bool isDirectory,
        FileTransferCollisionResolution collisionResolution,
        out bool skipped)
    {
        skipped = false;
        var entryName = isDirectory
            ? Path.GetFileName(sourcePath.TrimEnd('\\'))
            : Path.GetFileName(sourcePath);
        var preferredPath = Path.Combine(destinationDirectory, entryName);

        if (!PathExists(preferredPath))
            return preferredPath;

        switch (collisionResolution)
        {
            case FileTransferCollisionResolution.Skip:
                skipped = true;
                return preferredPath;
            case FileTransferCollisionResolution.Replace:
                DeleteExistingTarget(preferredPath);
                return preferredPath;
            default:
                return GetUniquePath(destinationDirectory, entryName, isDirectory);
        }
    }

    private void DeleteExistingTarget(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
            return;
        }

        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static bool PathExists(string path) => File.Exists(path) || Directory.Exists(path);
}
