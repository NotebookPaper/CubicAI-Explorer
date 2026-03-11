using CubicAIExplorer.Models;

namespace CubicAIExplorer.Services;

public sealed record ArchiveEntryInfo(string FullName, long Length, bool IsDirectory);

public enum FileTransferCollisionResolution
{
    KeepBoth,
    Replace,
    Skip,
    Rename
}

public enum FileTransferStatus
{
    Success,
    Skipped,
    Failed
}

public sealed record FileTransferResult(
    string SourcePath,
    string DestinationPath,
    bool IsDirectory,
    FileTransferStatus Status = FileTransferStatus.Success,
    string? ErrorMessage = null);

public interface IFileSystemService
{
    IReadOnlyList<FileSystemItem> GetDrives();
    IReadOnlyList<FileSystemItem> GetDirectoryContents(string path, bool showHidden = false);
    IReadOnlyList<FileSystemItem> GetSubDirectories(string path, bool showHidden = false);
    string GetDisplayName(string path);
    string? ResolveDirectoryPath(string path);
    bool DirectoryExists(string path);
    bool FileExists(string path);
    string GetParentPath(string path);
    void OpenFile(string path);
    void RevealInExplorer(string path);
    void OpenInDefaultApp(string path);
    IReadOnlyList<FileTransferResult> CopyFiles(
        IEnumerable<string> sourcePaths,
        string destinationDirectory,
        FileTransferCollisionResolution collisionResolution = FileTransferCollisionResolution.KeepBoth,
        IFileOperationContext? operationContext = null);
    IReadOnlyList<FileTransferResult> MoveFiles(
        IEnumerable<string> sourcePaths,
        string destinationDirectory,
        FileTransferCollisionResolution collisionResolution = FileTransferCollisionResolution.KeepBoth,
        IFileOperationContext? operationContext = null);
    void DeleteFiles(IEnumerable<string> paths, bool permanentDelete = false, IFileOperationContext? operationContext = null);
    string RenameFile(string path, string newName);
    string CreateFolder(string parentPath, string folderName);
    string CreateFile(string parentPath, string fileName);
    void CreateSymbolicLink(string linkPath, string targetPath);
    string? EnsureDirectoryExists(string path);
    IReadOnlyList<ArchiveEntryInfo> GetArchiveEntries(string archivePath, int maxEntries = 100);
    void ExtractArchive(string archivePath, string destinationDirectory, IFileOperationContext? operationContext = null);
    void ExtractArchiveEntries(
        string archivePath,
        string destinationDirectory,
        IEnumerable<string> entryPaths,
        IFileOperationContext? operationContext = null);
}
