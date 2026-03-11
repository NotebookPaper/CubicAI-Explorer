using System.IO;
using System.Text.Json;
using CubicAIExplorer.Models;

namespace CubicAIExplorer.Services;

public sealed class BookmarkService : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly FileSystemWatcher? _watcher;
    private DateTime _lastWriteTime;

    public event EventHandler<List<BookmarkItem>>? BookmarksChanged;

    public BookmarkService()
    {
        var path = GetBookmarksPath();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            _watcher = new FileSystemWatcher(dir, Path.GetFileName(path))
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };
            _watcher.Changed += OnFileChanged;
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var currentWrite = File.GetLastWriteTime(e.FullPath);
        if (currentWrite > _lastWriteTime.AddMilliseconds(500))
        {
            _lastWriteTime = currentWrite;
            var bookmarks = Load();
            BookmarksChanged?.Invoke(this, bookmarks);
        }
    }

    public static string GetBookmarksPath()
    {
        var overridePath = Environment.GetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH");
        if (!string.IsNullOrWhiteSpace(overridePath))
            return overridePath;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "CubicAIExplorer", "bookmarks.json");
    }

    public List<BookmarkItem> Load()
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
                Thread.Sleep(100);
            }
            catch
            {
                break;
            }
        }
        return [];
    }

    public void Save(IEnumerable<BookmarkItem> bookmarks)
    {
        try
        {
            var path = GetBookmarksPath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            if (_watcher != null) _watcher.EnableRaisingEvents = false;

            var records = bookmarks.Select(MapBookmarkToRecord).ToList();
            var json = JsonSerializer.Serialize(records, JsonOptions);

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                    using var writer = new StreamWriter(stream);
                    writer.Write(json);
                    _lastWriteTime = File.GetLastWriteTime(path);
                    break;
                }
                catch (IOException)
                {
                    Thread.Sleep(100);
                }
            }
        }
        catch { /* Non-critical */ }
        finally
        {
            if (_watcher != null) _watcher.EnableRaisingEvents = true;
        }
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
