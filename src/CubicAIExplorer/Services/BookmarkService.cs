using System.IO;
using System.Text.Json;
using CubicAIExplorer.Models;

namespace CubicAIExplorer.Services;

public sealed class BookmarkService : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
    private readonly DebouncedJsonFileWatcher<List<BookmarkItem>>? _watcher;

    public event EventHandler<List<BookmarkItem>>? BookmarksChanged;

    public BookmarkService()
    {
        var path = GetBookmarksPath();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            _watcher = new DebouncedJsonFileWatcher<List<BookmarkItem>>(
                path,
                Load,
                bookmarks => BookmarksChanged?.Invoke(this, bookmarks));
        }
    }

    public static string GetBookmarksPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var fallbackPath = Path.Combine(appData, "CubicAIExplorer", "bookmarks.json");
        var overridePath = Environment.GetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH");
        if (!string.IsNullOrWhiteSpace(overridePath))
            return PathSecurityHelper.SanitizePathOrFallback(overridePath, fallbackPath);

        return fallbackPath;
    }

    public List<BookmarkItem> Load()
    {
        var path = GetBookmarksPath();
        return LoadCore(path);
    }

    public Task<List<BookmarkItem>> LoadAsync()
    {
        var path = GetBookmarksPath();
        return LoadCoreAsync(path);
    }

    public void Save(IEnumerable<BookmarkItem> bookmarks)
    {
        SaveCore(bookmarks);
    }

    public Task SaveAsync(IEnumerable<BookmarkItem> bookmarks)
    {
        return SaveCoreAsync(bookmarks);
    }

    private static List<BookmarkItem> LoadCore(string path)
    {
        if (!File.Exists(path))
            return [];

        for (var i = 0; i < 3; i++)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                var records = JsonSerializer.Deserialize<List<BookmarkRecord>>(json);
                return records?.Select(MapRecordToBookmark).ToList() ?? [];
            }
            catch (IOException) when (i < 2)
            {
                Thread.Sleep(100);
            }
            catch
            {
                break;
            }
        }

        return [];
    }

    private static async Task<List<BookmarkItem>> LoadCoreAsync(string path)
    {
        if (!File.Exists(path))
            return [];

        for (var i = 0; i < 3; i++)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync().ConfigureAwait(false);
                var records = JsonSerializer.Deserialize<List<BookmarkRecord>>(json);
                return records?.Select(MapRecordToBookmark).ToList() ?? [];
            }
            catch (IOException) when (i < 2)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }
            catch
            {
                break;
            }
        }

        return [];
    }

    private void SaveCore(IEnumerable<BookmarkItem> bookmarks)
    {
        try
        {
            var path = GetBookmarksPath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            using var suppression = _watcher?.SuppressNotifications();

            var records = bookmarks.Select(MapBookmarkToRecord).ToList();
            var json = JsonSerializer.Serialize(records, JsonOptions);

            for (var i = 0; i < 3; i++)
            {
                try
                {
                    using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                    using var writer = new StreamWriter(stream);
                    writer.Write(json);
                    return;
                }
                catch (IOException) when (i < 2)
                {
                    Thread.Sleep(100);
                }
            }
        }
        catch
        {
            // Non-critical.
        }
    }

    private async Task SaveCoreAsync(IEnumerable<BookmarkItem> bookmarks)
    {
        try
        {
            var path = GetBookmarksPath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            using var suppression = _watcher?.SuppressNotifications();

            var records = bookmarks.Select(MapBookmarkToRecord).ToList();
            var json = JsonSerializer.Serialize(records, JsonOptions);

            for (var i = 0; i < 3; i++)
            {
                try
                {
                    using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                    using var writer = new StreamWriter(stream);
                    await writer.WriteAsync(json).ConfigureAwait(false);
                    return;
                }
                catch (IOException) when (i < 2)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }
        }
        catch
        {
            // Non-critical.
        }
    }

    private static BookmarkItem MapRecordToBookmark(BookmarkRecord record)
    {
        var item = new BookmarkItem
        {
            Name = record.Name,
            Path = record.Path,
            // Legacy files (before IsFolder was persisted) only stored folder
            // categories as path-less entries, so fall back to that inference.
            IsFolder = record.IsFolder ?? string.IsNullOrWhiteSpace(record.Path),
            IsExpanded = record.IsExpanded ?? false
        };
        if (record.Children != null)
        {
            foreach (var childRecord in record.Children)
            {
                item.Children.Add(MapRecordToBookmark(childRecord));
            }
        }

        return item;
    }

    private static BookmarkRecord MapBookmarkToRecord(BookmarkItem item)
    {
        var record = new BookmarkRecord
        {
            Name = item.Name,
            Path = item.Path,
            IsFolder = item.IsFolder,
            IsExpanded = item.IsFolder && item.IsExpanded ? true : null
        };
        if (item.Children.Count > 0)
        {
            record.Children = item.Children.Select(MapBookmarkToRecord).ToList();
        }

        return record;
    }

    public void Dispose()
    {
        _watcher?.Dispose();
    }

    private sealed class BookmarkRecord
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool? IsFolder { get; set; }
        public bool? IsExpanded { get; set; }
        public List<BookmarkRecord>? Children { get; set; }
    }
}
