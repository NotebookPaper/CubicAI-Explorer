using CubicAIExplorer.Models;

namespace CubicAIExplorer.Services;

public enum FileTransferCollisionResolution
{
    KeepBoth,
    Replace,
    Skip
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
    bool DirectoryExists(string path);
    bool FileExists(string path);
    string GetParentPath(string path);
    void OpenFile(string path);
    void OpenInDefaultApp(string path);
    IReadOnlyList<FileTransferResult> CopyFiles(
        IEnumerable<string> sourcePaths,
        string destinationDirectory,
        FileTransferCollisionResolution collisionResolution = FileTransferCollisionResolution.KeepBoth);
    IReadOnlyList<FileTransferResult> MoveFiles(
        IEnumerable<string> sourcePaths,
        string destinationDirectory,
        FileTransferCollisionResolution collisionResolution = FileTransferCollisionResolution.KeepBoth);
    void DeleteFiles(IEnumerable<string> paths, bool permanentDelete = false);
    string RenameFile(string path, string newName);
    string CreateFolder(string parentPath, string folderName);
}
