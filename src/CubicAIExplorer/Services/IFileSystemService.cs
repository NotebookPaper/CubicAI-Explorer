using CubicAIExplorer.Models;

namespace CubicAIExplorer.Services;

public sealed record FileTransferResult(string SourcePath, string DestinationPath, bool IsDirectory);

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
    IReadOnlyList<FileTransferResult> CopyFiles(IEnumerable<string> sourcePaths, string destinationDirectory);
    IReadOnlyList<FileTransferResult> MoveFiles(IEnumerable<string> sourcePaths, string destinationDirectory);
    void DeleteFiles(IEnumerable<string> paths, bool permanentDelete = false);
    string RenameFile(string path, string newName);
    string CreateFolder(string parentPath, string folderName);
}
