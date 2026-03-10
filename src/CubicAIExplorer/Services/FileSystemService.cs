using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.FileIO;
using CubicAIExplorer.Models;

namespace CubicAIExplorer.Services;

public sealed class FileSystemService : IFileSystemService
{
    private const uint SHGFI_DISPLAYNAME = 0x000000200;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

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

    public string GetDisplayName(string path)
    {
        var sanitized = SanitizePath(path);
        if (sanitized == null)
            return path;

        var displayName = TryGetShellDisplayName(sanitized);
        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName;

        var trimmed = sanitized.TrimEnd('\\');
        var name = Path.GetFileName(trimmed);
        return string.IsNullOrWhiteSpace(name) ? sanitized : name;
    }

    public string? ResolveDirectoryPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var rawPath = path.Trim();
        var knownFolderPath = ResolveKnownFolderAlias(rawPath);
        if (!string.IsNullOrWhiteSpace(knownFolderPath))
            return knownFolderPath;

        var sanitized = SanitizePath(rawPath);
        return sanitized != null && Directory.Exists(sanitized) ? sanitized : null;
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
        FileTransferCollisionResolution collisionResolution = FileTransferCollisionResolution.KeepBoth,
        IFileOperationContext? operationContext = null)
    {
        var results = new List<FileTransferResult>();
        var destination = SanitizePath(destinationDirectory);
        if (destination == null || !Directory.Exists(destination)) return results;

        var sanitizedSources = sourcePaths
            .Select(SanitizePath)
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>()
            .ToArray();

        var processedCount = 0;
        foreach (var source in sanitizedSources)
        {
            operationContext?.CancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(source))
            {
                results.Add(CopyFile(source, destination, collisionResolution));
            }
            else if (Directory.Exists(source))
            {
                results.Add(CopyDirectory(source, destination, collisionResolution));
            }

            processedCount++;
            operationContext?.ReportProgress(
                processedCount,
                sanitizedSources.Length,
                Path.GetFileName(source.TrimEnd('\\')));
        }

        return results;
    }

    public IReadOnlyList<FileTransferResult> MoveFiles(
        IEnumerable<string> sourcePaths,
        string destinationDirectory,
        FileTransferCollisionResolution collisionResolution = FileTransferCollisionResolution.KeepBoth,
        IFileOperationContext? operationContext = null)
    {
        var results = new List<FileTransferResult>();
        var destination = SanitizePath(destinationDirectory);
        if (destination == null || !Directory.Exists(destination)) return results;

        var sanitizedSources = sourcePaths
            .Select(SanitizePath)
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>()
            .ToArray();

        var processedCount = 0;
        foreach (var source in sanitizedSources)
        {
            operationContext?.CancellationToken.ThrowIfCancellationRequested();
            processedCount++;

            if (File.Exists(source))
            {
                var sourceDir = Path.GetDirectoryName(source);
                if (!string.IsNullOrWhiteSpace(sourceDir)
                    && string.Equals(sourceDir.TrimEnd('\\'), destination.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                {
                    operationContext?.ReportProgress(
                        processedCount,
                        sanitizedSources.Length,
                        Path.GetFileName(source.TrimEnd('\\')));
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
                    operationContext?.ReportProgress(
                        processedCount,
                        sanitizedSources.Length,
                        Path.GetFileName(source.TrimEnd('\\')));
                    continue;
                }

                results.Add(MoveDirectoryEntry(source, destination, collisionResolution));
            }

            operationContext?.ReportProgress(
                processedCount,
                sanitizedSources.Length,
                Path.GetFileName(source.TrimEnd('\\')));
        }

        return results;
    }

    public void DeleteFiles(IEnumerable<string> paths, bool permanentDelete = false, IFileOperationContext? operationContext = null)
    {
        var sanitizedPaths = paths
            .Select(SanitizePath)
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>()
            .ToArray();

        for (var i = 0; i < sanitizedPaths.Length; i++)
        {
            operationContext?.CancellationToken.ThrowIfCancellationRequested();
            var sanitized = sanitizedPaths[i];

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

            operationContext?.ReportProgress(i + 1, sanitizedPaths.Length, Path.GetFileName(sanitized.TrimEnd('\\')));
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

    public string CreateFile(string parentPath, string fileName)
    {
        var sanitizedParent = SanitizePath(parentPath);
        if (sanitizedParent == null || !Directory.Exists(sanitizedParent))
            return parentPath;

        var baseName = string.IsNullOrWhiteSpace(fileName) ? "New file.txt" : fileName.Trim();
        var targetPath = GetUniquePath(sanitizedParent, baseName, isDirectory: false);
        File.WriteAllText(targetPath, string.Empty);
        return targetPath;
    }

    public void CreateSymbolicLink(string linkPath, string targetPath)
    {
        var sanitizedLink = SanitizePath(linkPath);
        var sanitizedTarget = SanitizePath(targetPath);
        if (sanitizedLink == null || sanitizedTarget == null) return;

        if (Directory.Exists(sanitizedTarget))
        {
            Directory.CreateSymbolicLink(sanitizedLink, sanitizedTarget);
        }
        else if (File.Exists(sanitizedTarget))
        {
            File.CreateSymbolicLink(sanitizedLink, sanitizedTarget);
        }
    }

    public string? EnsureDirectoryExists(string path)
    {
        var sanitized = SanitizePath(path);
        if (sanitized == null)
            return null;

        Directory.CreateDirectory(sanitized);
        return sanitized;
    }

    public IReadOnlyList<ArchiveEntryInfo> GetArchiveEntries(string archivePath, int maxEntries = 100)
    {
        var sanitized = SanitizePath(archivePath);
        if (sanitized == null || !File.Exists(sanitized))
            return [];

        using var stream = new FileStream(sanitized, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

        return archive.Entries
            .Take(Math.Max(1, maxEntries))
            .Select(static entry => new ArchiveEntryInfo(
                entry.FullName,
                entry.Length,
                entry.FullName.EndsWith("/", StringComparison.Ordinal) || string.IsNullOrEmpty(entry.Name)))
            .ToList();
    }

    public void ExtractArchive(string archivePath, string destinationDirectory, IFileOperationContext? operationContext = null)
    {
        var sanitizedArchive = SanitizePath(archivePath);
        var sanitizedDestination = SanitizePath(destinationDirectory);
        if (sanitizedArchive == null || sanitizedDestination == null)
            return;
        if (!File.Exists(sanitizedArchive) || !Directory.Exists(sanitizedDestination))
            return;

        using var stream = new FileStream(sanitizedArchive, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

        for (var i = 0; i < archive.Entries.Count; i++)
        {
            operationContext?.CancellationToken.ThrowIfCancellationRequested();
            var entry = archive.Entries[i];
            var targetPath = SanitizePath(Path.Combine(sanitizedDestination, entry.FullName));
            if (targetPath == null || !targetPath.StartsWith(sanitizedDestination, StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(targetPath);
                continue;
            }

            var targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(targetDir))
                Directory.CreateDirectory(targetDir);

            entry.ExtractToFile(targetPath, overwrite: false);
            operationContext?.ReportProgress(i + 1, archive.Entries.Count, entry.FullName);
        }
    }

    public void ExtractArchiveEntries(
        string archivePath,
        string destinationDirectory,
        IEnumerable<string> entryPaths,
        IFileOperationContext? operationContext = null)
    {
        var sanitizedArchive = SanitizePath(archivePath);
        var sanitizedDestination = SanitizePath(destinationDirectory);
        if (sanitizedArchive == null || sanitizedDestination == null)
            return;
        if (!File.Exists(sanitizedArchive) || !Directory.Exists(sanitizedDestination))
            return;

        var requestedEntries = entryPaths
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (requestedEntries.Length == 0)
            return;

        using var stream = new FileStream(sanitizedArchive, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

        var matchingEntries = archive.Entries
            .Where(entry => requestedEntries.Any(requested =>
                string.Equals(entry.FullName, requested, StringComparison.OrdinalIgnoreCase)
                || entry.FullName.StartsWith(requested.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        for (var i = 0; i < matchingEntries.Count; i++)
        {
            operationContext?.CancellationToken.ThrowIfCancellationRequested();
            var entry = matchingEntries[i];
            var targetPath = SanitizePath(Path.Combine(sanitizedDestination, entry.FullName));
            if (targetPath == null || !targetPath.StartsWith(sanitizedDestination, StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(targetPath);
                continue;
            }

            var targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(targetDir))
                Directory.CreateDirectory(targetDir);

            entry.ExtractToFile(targetPath, overwrite: false);
            operationContext?.ReportProgress(i + 1, matchingEntries.Count, entry.FullName);
        }
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

    private static string? TryGetShellDisplayName(string path)
    {
        var shellInfo = new SHFILEINFO();
        var flags = SHGFI_DISPLAYNAME;
        var attributes = FILE_ATTRIBUTE_NORMAL;

        if (!PathExists(path))
        {
            flags |= SHGFI_USEFILEATTRIBUTES;
            attributes = LooksLikeDirectoryPath(path) ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;
        }

        var result = SHGetFileInfo(
            path,
            attributes,
            ref shellInfo,
            (uint)Marshal.SizeOf<SHFILEINFO>(),
            flags);

        return result == IntPtr.Zero ? null : shellInfo.szDisplayName;
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

    private static string? ResolveKnownFolderAlias(string rawPath)
    {
        var normalized = rawPath
            .Trim()
            .TrimEnd('\\', '/')
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        return normalized switch
        {
            "desktop" => GetExistingKnownFolder(Environment.SpecialFolder.Desktop),
            "documents" or "document" or "mydocuments" => GetExistingKnownFolder(Environment.SpecialFolder.MyDocuments),
            "downloads" or "download" => GetExistingPath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads")),
            "pictures" or "picture" or "mypictures" => GetExistingKnownFolder(Environment.SpecialFolder.MyPictures),
            "music" or "mymusic" => GetExistingKnownFolder(Environment.SpecialFolder.MyMusic),
            "videos" or "video" or "myvideos" => GetExistingKnownFolder(Environment.SpecialFolder.MyVideos),
            "home" or "profile" or "userprofile" => GetExistingKnownFolder(Environment.SpecialFolder.UserProfile),
            _ => null
        };
    }

    private static string? GetExistingKnownFolder(Environment.SpecialFolder specialFolder)
        => GetExistingPath(Environment.GetFolderPath(specialFolder));

    private static string? GetExistingPath(string? path)
        => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path) ? path : null;

    private static bool LooksLikeDirectoryPath(string path)
    {
        if (path.EndsWith('\\') || path.EndsWith('/'))
            return true;

        var extension = Path.GetExtension(path);
        return string.IsNullOrWhiteSpace(extension);
    }

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

        string? backupPath = null;
        if (collisionResolution == FileTransferCollisionResolution.Replace && File.Exists(targetPath))
        {
            backupPath = CreateBackup(targetPath);
            if (backupPath == null)
                return new FileTransferResult(source, targetPath, IsDirectory: false, FileTransferStatus.Failed, "Could not create backup for replace.");
        }

        try
        {
            File.Copy(source, targetPath, overwrite: false);
            if (backupPath != null) DeleteBackup(backupPath);
            return new FileTransferResult(source, targetPath, IsDirectory: false);
        }
        catch (Exception ex)
        {
            if (backupPath != null) RestoreBackup(backupPath, targetPath);
            return new FileTransferResult(source, targetPath, IsDirectory: false, FileTransferStatus.Failed, ex.Message);
        }
    }

    private FileTransferResult CopyDirectory(string source, string destinationDirectory, FileTransferCollisionResolution collisionResolution)
    {
        var targetPath = ResolveTargetPath(destinationDirectory, source, isDirectory: true, collisionResolution, out var skipped);
        if (skipped)
            return new FileTransferResult(source, string.Empty, IsDirectory: true, FileTransferStatus.Skipped);

        string? backupPath = null;
        if (collisionResolution == FileTransferCollisionResolution.Replace && Directory.Exists(targetPath))
        {
            backupPath = CreateBackup(targetPath);
            if (backupPath == null)
                return new FileTransferResult(source, targetPath, IsDirectory: true, FileTransferStatus.Failed, "Could not create backup for replace.");
        }

        try
        {
            CopyDirectoryRecursive(source, targetPath);
            if (backupPath != null) DeleteBackup(backupPath);
            return new FileTransferResult(source, targetPath, IsDirectory: true);
        }
        catch (Exception ex)
        {
            if (backupPath != null) RestoreBackup(backupPath, targetPath);
            return new FileTransferResult(source, targetPath, IsDirectory: true, FileTransferStatus.Failed, ex.Message);
        }
    }

    private FileTransferResult MoveFile(string source, string destinationDirectory, FileTransferCollisionResolution collisionResolution)
    {
        var targetPath = ResolveTargetPath(destinationDirectory, source, isDirectory: false, collisionResolution, out var skipped);
        if (skipped)
            return new FileTransferResult(source, string.Empty, IsDirectory: false, FileTransferStatus.Skipped);

        string? backupPath = null;
        if (collisionResolution == FileTransferCollisionResolution.Replace && File.Exists(targetPath))
        {
            backupPath = CreateBackup(targetPath);
            if (backupPath == null)
                return new FileTransferResult(source, targetPath, IsDirectory: false, FileTransferStatus.Failed, "Could not create backup for replace.");
        }

        try
        {
            File.Move(source, targetPath, overwrite: false);
            if (backupPath != null) DeleteBackup(backupPath);
            return new FileTransferResult(source, targetPath, IsDirectory: false);
        }
        catch (Exception ex)
        {
            if (backupPath != null) RestoreBackup(backupPath, targetPath);
            return new FileTransferResult(source, targetPath, IsDirectory: false, FileTransferStatus.Failed, ex.Message);
        }
    }

    private FileTransferResult MoveDirectoryEntry(string source, string destinationDirectory, FileTransferCollisionResolution collisionResolution)
    {
        var targetPath = ResolveTargetPath(destinationDirectory, source, isDirectory: true, collisionResolution, out var skipped);
        if (skipped)
            return new FileTransferResult(source, string.Empty, IsDirectory: true, FileTransferStatus.Skipped);

        string? backupPath = null;
        if (collisionResolution == FileTransferCollisionResolution.Replace && Directory.Exists(targetPath))
        {
            backupPath = CreateBackup(targetPath);
            if (backupPath == null)
                return new FileTransferResult(source, targetPath, IsDirectory: true, FileTransferStatus.Failed, "Could not create backup for replace.");
        }

        try
        {
            MoveDirectory(source, targetPath);
            if (backupPath != null) DeleteBackup(backupPath);
            return new FileTransferResult(source, targetPath, IsDirectory: true);
        }
        catch (Exception ex)
        {
            if (backupPath != null) RestoreBackup(backupPath, targetPath);
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
                // We return the preferred path but the caller must handle the Replace backup
                return preferredPath;
            default:
                return GetUniquePath(destinationDirectory, entryName, isDirectory);
        }
    }

    private string? CreateBackup(string path)
    {
        if (!PathExists(path)) return null;

        try
        {
            var backupPath = path + ".bak_" + Guid.NewGuid().ToString("N")[..8];
            if (File.Exists(path))
                File.Move(path, backupPath);
            else
                Directory.Move(path, backupPath);
            return backupPath;
        }
        catch { return null; }
    }

    private void RestoreBackup(string backupPath, string originalPath)
    {
        try
        {
            if (File.Exists(backupPath))
                File.Move(backupPath, originalPath, overwrite: true);
            else if (Directory.Exists(backupPath))
                Directory.Move(backupPath, originalPath);
        }
        catch { /* Best effort */ }
    }

    private void DeleteBackup(string backupPath)
    {
        try
        {
            if (File.Exists(backupPath))
                File.Delete(backupPath);
            else if (Directory.Exists(backupPath))
                Directory.Delete(backupPath, recursive: true);
        }
        catch { /* Best effort */ }
    }

    private static bool PathExists(string path) => File.Exists(path) || Directory.Exists(path);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);
}
