using CubicAIExplorer.Services;
using CubicAIExplorer.ViewModels;

namespace CubicAIExplorer.Models;

public sealed record ArchiveBrowseRequest(
    string ArchivePath,
    IReadOnlyList<ArchiveEntryInfo> Entries,
    FileListViewModel SourceFileList,
    IFileSystemService FileSystemService);
