using System.IO;

namespace CubicAIExplorer.Services;

internal sealed class DebouncedJsonFileWatcher<T> : IDisposable
{
    private static readonly TimeSpan DefaultDebounceDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan SaveSuppressionWindow = TimeSpan.FromMilliseconds(750);

    private readonly object _gate = new();
    private readonly string _path;
    private readonly string _directoryPath;
    private readonly string _fileName;
    private readonly Func<T> _loadCallback;
    private readonly Action<T> _changedCallback;
    private readonly Timer _debounceTimer;

    private FileSystemWatcher? _watcher;
    private bool _isDisposed;
    private bool _notificationsSuspended;
    private DateTime _suppressUntilUtc = DateTime.MinValue;

    public DebouncedJsonFileWatcher(string path, Func<T> loadCallback, Action<T> changedCallback)
    {
        _path = path;
        _directoryPath = Path.GetDirectoryName(path) ?? string.Empty;
        _fileName = Path.GetFileName(path);
        _loadCallback = loadCallback;
        _changedCallback = changedCallback;
        _debounceTimer = new Timer(OnDebounceTimerElapsed);

        if (!string.IsNullOrWhiteSpace(_directoryPath))
        {
            Directory.CreateDirectory(_directoryPath);
            CreateWatcher();
        }
    }

    public IDisposable SuppressNotifications()
    {
        lock (_gate)
        {
            if (_isDisposed)
                return NoopDisposable.Instance;

            _notificationsSuspended = true;
            _suppressUntilUtc = DateTime.UtcNow.Add(SaveSuppressionWindow);
            if (_watcher != null)
                _watcher.EnableRaisingEvents = false;
        }

        return new ResumeScope(this);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _watcher?.Dispose();
            _watcher = null;
        }

        _debounceTimer.Dispose();
    }

    private void ResumeNotifications()
    {
        lock (_gate)
        {
            if (_isDisposed)
                return;

            _notificationsSuspended = false;
            if (_watcher != null)
                _watcher.EnableRaisingEvents = true;
        }
    }

    private void CreateWatcher()
    {
        _watcher = new FileSystemWatcher(_directoryPath, _fileName)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnWatchedFileChanged;
        _watcher.Created += OnWatchedFileChanged;
        _watcher.Deleted += OnWatchedFileChanged;
        _watcher.Renamed += OnWatchedFileRenamed;
        _watcher.Error += OnWatcherError;
    }

    private void RecreateWatcher()
    {
        lock (_gate)
        {
            if (_isDisposed || string.IsNullOrWhiteSpace(_directoryPath))
                return;

            _watcher?.Dispose();
            _watcher = null;

            Directory.CreateDirectory(_directoryPath);
            CreateWatcher();
            if (_notificationsSuspended && _watcher != null)
                _watcher.EnableRaisingEvents = false;
        }
    }

    private void OnWatchedFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!IsTargetPath(e.FullPath))
            return;

        ScheduleReload(DefaultDebounceDelay);
    }

    private void OnWatchedFileRenamed(object sender, RenamedEventArgs e)
    {
        if (!IsTargetPath(e.FullPath) && !IsTargetPath(e.OldFullPath))
            return;

        ScheduleReload(DefaultDebounceDelay);
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        RecreateWatcher();
        ScheduleReload(TimeSpan.Zero);
    }

    private bool IsTargetPath(string? candidatePath)
        => string.Equals(candidatePath, _path, StringComparison.OrdinalIgnoreCase);

    private void ScheduleReload(TimeSpan delay)
    {
        lock (_gate)
        {
            if (_isDisposed || _notificationsSuspended || DateTime.UtcNow < _suppressUntilUtc)
                return;

            _debounceTimer.Change(delay, Timeout.InfiniteTimeSpan);
        }
    }

    private void OnDebounceTimerElapsed(object? state)
    {
        if (IsSuppressed())
            return;

        try
        {
            var data = _loadCallback();
            _changedCallback(data);
        }
        catch
        {
            // Watch reloads are best-effort; the next file event will retry.
        }
    }

    private bool IsSuppressed()
    {
        lock (_gate)
        {
            return _isDisposed || _notificationsSuspended || DateTime.UtcNow < _suppressUntilUtc;
        }
    }

    private sealed class ResumeScope : IDisposable
    {
        private DebouncedJsonFileWatcher<T>? _owner;

        public ResumeScope(DebouncedJsonFileWatcher<T> owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _owner, null)?.ResumeNotifications();
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
