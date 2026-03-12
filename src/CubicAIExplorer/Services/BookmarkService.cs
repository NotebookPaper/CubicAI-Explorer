using System.IO;
using System.Text.Json;
using CubicAIExplorer.Models;

namespace CubicAIExplorer.Services;

public sealed class BookmarkService : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
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
        => LoadAsync().GetAwaiter().GetResult();

    public async Task<List<BookmarkItem>> LoadAsync()
    {
        var path = GetBookmarksPath();
        if (!File.Exists(path)) return [];

        for (int i = 0; i < 3; i++)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                var records = JsonSerializer.Deserialize<List<BookmarkRecord>>(json);
                return records?.Select(MapRecordToBookmark).ToList() ?? [];
            }
            catch (IOException)
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

    public void Save(IEnumerable<BookmarkItem> bookmarks)
        => SaveAsync(bookmarks).GetAwaiter().GetResult();

    public async Task SaveAsync(IEnumerable<BookmarkItem> bookmarks)
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

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                    using var writer = new StreamWriter(stream);
                    writer.Write(json);
                    break;
                }
                catch (IOException)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }
        }
        catch { /* Non-critical */ }
    }

    private BookmarkItem MapRecordToBookmark(BookmarkRecord record)
    {
        var item = new BookmarkItem
        {
            Name = record.Name,
            Path = record.Path,
            IsFolder = true
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

    private BookmarkRecord MapBookmarkToRecord(BookmarkItem item)
    {
        var record = new BookmarkRecord
        {
            Name = item.Name,
            Path = item.Path
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
        public List<BookmarkRecord>? Children { get; set; }
    }
}
