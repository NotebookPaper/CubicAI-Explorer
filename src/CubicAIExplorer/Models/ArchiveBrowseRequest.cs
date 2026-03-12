using CubicAIExplorer.Services;

namespace CubicAIExplorer.Models;

public sealed record ArchiveBrowseRequest(
    string ArchivePath,
    IReadOnlyList<ArchiveEntryInfo> Entries,
    Func<IEnumerable<string>, string, bool, Task> ExtractEntriesAsync);
