using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CubicAIExplorer.Models;
using CubicAIExplorer.Services;

namespace CubicAIExplorer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private static readonly JsonSerializerOptions BookmarkJsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly IFileSystemService _fileSystemService;
    private readonly IClipboardService _clipboardService;

    [ObservableProperty]
    private TabViewModel? _activeTab;

    [ObservableProperty]
    private FolderTreeNodeViewModel? _selectedTreeNode;

    [ObservableProperty]
    private string _addressBarText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private BookmarkItem? _selectedBookmark;

    // Dual pane
    [ObservableProperty]
    private bool _isDualPaneMode;

    [ObservableProperty]
    private string _rightPaneAddressText = string.Empty;

    private TabViewModel? _rightPaneTab;
    public TabViewModel? RightPaneTab => _rightPaneTab;

    // Preview
    [ObservableProperty]
    private bool _isPreviewVisible;

    [ObservableProperty]
    private string _previewFileName = string.Empty;

    [ObservableProperty]
    private string _previewFileInfo = string.Empty;

    [ObservableProperty]
    private string _previewText = string.Empty;

    [ObservableProperty]
    private bool _hasPreviewText;

    [ObservableProperty]
    private ImageSource? _previewImageSource;

    [ObservableProperty]
    private bool _hasPreviewImage;

    // Address autocomplete
    [ObservableProperty]
    private bool _isAddressSuggestionsOpen;

    public ObservableCollection<TabViewModel> Tabs { get; } = [];
    public ObservableCollection<FolderTreeNodeViewModel> FolderTreeRoots { get; } = [];
    public ObservableCollection<BookmarkItem> Bookmarks { get; } = [];
    public ObservableCollection<BreadcrumbSegment> BreadcrumbSegments { get; } = [];
    public ObservableCollection<RecentFolderItem> RecentFolders { get; } = [];
    public ObservableCollection<string> AddressSuggestions { get; } = [];

    public event EventHandler? DualPaneModeChanged;
    public event EventHandler? PreviewModeChanged;

    private const int MaxRecentFolders = 15;

    public MainViewModel(IFileSystemService fileSystemService, IClipboardService clipboardService)
    {
        _fileSystemService = fileSystemService;
        _clipboardService = clipboardService;
        LoadBookmarks();
        LoadRecentFolders();
        LoadDrives();
    }

    private void AttachTab(TabViewModel tab)
    {
        tab.PropertyChanged += OnTabPropertyChanged;
        tab.FileList.PropertyChanged += OnFileListPropertyChanged;
    }

    private void OnTabPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not TabViewModel tab)
            return;

        if (e.PropertyName == nameof(TabViewModel.CurrentPath) && tab == ActiveTab)
        {
            AddressBarText = tab.CurrentPath;
            StatusText = tab.FileList.StatusText;
            UpdateBreadcrumbs(tab.CurrentPath);
            AddToRecentFolders(tab.CurrentPath);
            UpdatePreview();
        }

        if (e.PropertyName == nameof(TabViewModel.CurrentPath) && tab == _rightPaneTab)
        {
            RightPaneAddressText = tab.CurrentPath;
        }
    }

    private void OnFileListPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not FileListViewModel fileList)
            return;

        if (e.PropertyName == nameof(FileListViewModel.StatusText) && ActiveTab?.FileList == fileList)
        {
            StatusText = fileList.StatusText;
        }

        if ((e.PropertyName == nameof(FileListViewModel.SelectedItem)
                || e.PropertyName == nameof(FileListViewModel.CurrentPath))
            && ActiveTab?.FileList == fileList)
        {
            UpdatePreview();
        }
    }

    private static string GetBookmarksPath()
    {
        var overridePath = Environment.GetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH");
        if (!string.IsNullOrWhiteSpace(overridePath))
            return overridePath;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "CubicAIExplorer", "bookmarks.json");
    }

    private void LoadBookmarks()
    {
        var bookmarksPath = GetBookmarksPath();
        try
        {
            if (File.Exists(bookmarksPath))
            {
                var json = File.ReadAllText(bookmarksPath);
                var bookmarks = JsonSerializer.Deserialize<List<BookmarkRecord>>(json);
                if (bookmarks is { Count: > 0 })
                {
                    foreach (var bookmark in bookmarks)
                    {
                        if (string.IsNullOrWhiteSpace(bookmark.Path)) continue;
                        TryAddBookmark(bookmark.Path, bookmark.Name, save: false);
                    }

                    if (Bookmarks.Count > 0)
                        return;
                }
            }
        }
        catch
        {
            // Fall back to defaults if persisted bookmarks are unavailable or invalid.
        }

        TryAddBookmark(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), save: false);
        TryAddBookmark(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), save: false);
        TryAddBookmark(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads"), save: false);
        SaveBookmarks();
    }

    private void LoadDrives()
    {
        FolderTreeRoots.Clear();
        var drives = _fileSystemService.GetDrives();
        foreach (var drive in drives)
        {
            FolderTreeRoots.Add(FolderTreeNodeViewModel.CreateDriveNode(
                _fileSystemService, drive.Name, drive.FullPath));
        }
    }

    [RelayCommand]
    private void NewTab()
    {
        var tab = new TabViewModel(_fileSystemService, _clipboardService);
        AttachTab(tab);
        Tabs.Add(tab);
        ActiveTab = tab;

        // Navigate to user profile by default
        var defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        tab.NavigateTo(defaultPath);
    }

    [RelayCommand]
    private void CloseTab(TabViewModel? tab)
    {
        if (tab == null) return;
        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);

        if (Tabs.Count == 0)
        {
            NewTab();
        }
        else if (ActiveTab == tab)
        {
            ActiveTab = Tabs[Math.Min(index, Tabs.Count - 1)];
        }
    }

    [RelayCommand]
    private void NavigateToAddress()
    {
        if (ActiveTab == null || string.IsNullOrWhiteSpace(AddressBarText)) return;
        if (_fileSystemService.DirectoryExists(AddressBarText))
        {
            ActiveTab.NavigateTo(AddressBarText);
        }
    }

    [RelayCommand]
    private void NavigateUp()
    {
        if (ActiveTab == null) return;
        var parent = _fileSystemService.GetParentPath(ActiveTab.CurrentPath);
        if (parent != ActiveTab.CurrentPath)
        {
            ActiveTab.NavigateTo(parent);
        }
    }

    public void DuplicateTab(TabViewModel sourceTab)
    {
        NewTab();
        if (!string.IsNullOrWhiteSpace(sourceTab.CurrentPath))
            ActiveTab!.NavigateTo(sourceTab.CurrentPath);
    }

    public void CloseOtherTabs(TabViewModel keepTab)
    {
        var toClose = Tabs.Where(t => t != keepTab).ToList();
        foreach (var tab in toClose)
            Tabs.Remove(tab);

        ActiveTab = keepTab;
    }

    public void NavigateToPath(string path)
    {
        if (ActiveTab == null) NewTab();
        ActiveTab!.NavigateTo(path);
    }

    partial void OnActiveTabChanged(TabViewModel? value)
    {
        if (value != null)
        {
            AddressBarText = value.CurrentPath;
            StatusText = value.FileList.StatusText;
            UpdateBreadcrumbs(value.CurrentPath);
            UpdatePreview();
        }
    }

    public void SelectTreeNode(FolderTreeNodeViewModel node)
    {
        NavigateToPath(node.FullPath);
    }

    private void UpdateBreadcrumbs(string path)
    {
        BreadcrumbSegments.Clear();
        if (string.IsNullOrWhiteSpace(path)) return;

        var segments = new List<BreadcrumbSegment>();
        var current = path.TrimEnd('\\');

        while (!string.IsNullOrEmpty(current))
        {
            var name = Path.GetFileName(current);
            if (string.IsNullOrEmpty(name))
                name = current; // Drive root like "C:\"

            segments.Add(new BreadcrumbSegment
            {
                DisplayName = name,
                FullPath = current.Length == 2 && current[1] == ':' ? current + "\\" : current
            });

            var parent = Path.GetDirectoryName(current);
            if (parent == current) break;
            current = parent ?? string.Empty;
        }

        segments.Reverse();
        for (var i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            BreadcrumbSegments.Add(new BreadcrumbSegment
            {
                DisplayName = seg.DisplayName,
                FullPath = seg.FullPath,
                IsFirst = i == 0
            });
        }
    }

    private void AddToRecentFolders(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        // Remove if already present
        var existing = RecentFolders.FirstOrDefault(
            r => string.Equals(r.FullPath, path, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            RecentFolders.Remove(existing);

        // Add to front
        var trimmed = path.TrimEnd('\\');
        var name = Path.GetFileName(trimmed);
        if (string.IsNullOrEmpty(name)) name = path;

        RecentFolders.Insert(0, new RecentFolderItem
        {
            DisplayName = name,
            FullPath = path
        });

        // Trim excess
        while (RecentFolders.Count > MaxRecentFolders)
            RecentFolders.RemoveAt(RecentFolders.Count - 1);

        SaveRecentFolders();
    }

    [RelayCommand]
    private void AddBookmark()
    {
        if (ActiveTab == null || string.IsNullOrWhiteSpace(ActiveTab.CurrentPath)) return;
        TryAddBookmark(ActiveTab.CurrentPath);
    }

    [RelayCommand]
    private void RemoveBookmark(BookmarkItem? bookmark)
    {
        if (bookmark == null) return;
        Bookmarks.Remove(bookmark);
        if (SelectedBookmark == bookmark)
            SelectedBookmark = null;
        SaveBookmarks();
    }

    [RelayCommand]
    private void NavigateBookmark(BookmarkItem? bookmark)
    {
        if (bookmark == null) return;
        if (_fileSystemService.DirectoryExists(bookmark.Path))
        {
            NavigateToPath(bookmark.Path);
        }
    }

    private void TryAddBookmark(string path, string? displayName = null, bool save = true)
    {
        if (!_fileSystemService.DirectoryExists(path)) return;
        if (Bookmarks.Any(b => string.Equals(b.Path, path, StringComparison.OrdinalIgnoreCase))) return;

        var trimmedPath = path.TrimEnd('\\');
        var name = string.IsNullOrWhiteSpace(displayName)
            ? Path.GetFileName(trimmedPath)
            : displayName;
        if (string.IsNullOrWhiteSpace(name))
            name = path;

        Bookmarks.Add(new BookmarkItem
        {
            Name = name,
            Path = path,
            IsFolder = true
        });

        if (save)
            SaveBookmarks();
    }

    private void SaveBookmarks()
    {
        try
        {
            var bookmarksPath = GetBookmarksPath();
            var directory = Path.GetDirectoryName(bookmarksPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var payload = Bookmarks
                .Select(static b => new BookmarkRecord(b.Name, b.Path))
                .ToList();

            File.WriteAllText(bookmarksPath, JsonSerializer.Serialize(payload, BookmarkJsonOptions));
        }
        catch
        {
            // Persistence failures should not interrupt core app behavior.
        }
    }

    private sealed record BookmarkRecord(string Name, string Path);

    // --- Dual Pane ---

    [RelayCommand]
    private void ToggleDualPane()
    {
        IsDualPaneMode = !IsDualPaneMode;
        if (IsDualPaneMode && _rightPaneTab == null)
        {
            _rightPaneTab = new TabViewModel(_fileSystemService, _clipboardService);
            AttachTab(_rightPaneTab);
            OnPropertyChanged(nameof(RightPaneTab));

            if (ActiveTab != null && !string.IsNullOrWhiteSpace(ActiveTab.CurrentPath))
                _rightPaneTab.NavigateTo(ActiveTab.CurrentPath);
        }

        DualPaneModeChanged?.Invoke(this, EventArgs.Empty);
    }

    // --- Preview ---

    [RelayCommand]
    private void TogglePreview()
    {
        IsPreviewVisible = !IsPreviewVisible;
        PreviewModeChanged?.Invoke(this, EventArgs.Empty);
        if (IsPreviewVisible)
            UpdatePreview();
    }

    public void UpdatePreview()
    {
        PreviewFileName = string.Empty;
        PreviewFileInfo = string.Empty;
        PreviewText = string.Empty;
        HasPreviewText = false;
        PreviewImageSource = null;
        HasPreviewImage = false;

        if (!IsPreviewVisible) return;

        var item = ActiveTab?.FileList.SelectedItem;
        if (item == null) return;

        PreviewFileName = item.Name;

        if (item.ItemType == FileSystemItemType.Directory)
        {
            PreviewFileInfo = "File folder";
            return;
        }

        PreviewFileInfo = $"{item.TypeDescription}\n{item.DisplaySize}";

        var ext = item.Extension.ToLowerInvariant();

        // Image preview
        if (ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".ico")
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(item.FullPath);
                bitmap.DecodePixelWidth = 280;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                PreviewImageSource = bitmap;
                HasPreviewImage = true;
            }
            catch
            {
                // Image loading can fail for corrupt files.
            }
        }
        // Text preview
        else if (IsTextExtension(ext))
        {
            try
            {
                var lines = File.ReadLines(item.FullPath).Take(100);
                PreviewText = string.Join("\n", lines);
                HasPreviewText = true;
            }
            catch
            {
                // File may be locked or inaccessible.
            }
        }
    }

    private static bool IsTextExtension(string ext) => ext is
        ".txt" or ".cs" or ".xml" or ".json" or ".md" or ".log" or
        ".ini" or ".cfg" or ".yaml" or ".yml" or ".html" or ".htm" or
        ".css" or ".js" or ".ts" or ".py" or ".bat" or ".cmd" or ".ps1" or
        ".xaml" or ".csproj" or ".sln" or ".gitignore" or ".editorconfig" or
        ".csv" or ".tsv" or ".sql" or ".sh" or ".toml" or ".env" or ".h" or
        ".c" or ".cpp" or ".java" or ".go" or ".rs" or ".rb" or ".php";

    // --- Address Autocomplete ---

    public void UpdateAddressSuggestions()
    {
        AddressSuggestions.Clear();
        var text = AddressBarText;
        if (string.IsNullOrWhiteSpace(text))
        {
            IsAddressSuggestionsOpen = false;
            return;
        }

        string parentDir;
        string prefix;

        if (text.EndsWith('\\') || text.EndsWith('/'))
        {
            parentDir = text;
            prefix = string.Empty;
        }
        else
        {
            parentDir = Path.GetDirectoryName(text) ?? string.Empty;
            prefix = Path.GetFileName(text);
        }

        if (!_fileSystemService.DirectoryExists(parentDir))
        {
            IsAddressSuggestionsOpen = false;
            return;
        }

        try
        {
            var dirs = _fileSystemService.GetSubDirectories(parentDir)
                .Where(d => string.IsNullOrEmpty(prefix)
                    || d.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Take(15)
                .Select(d => d.FullPath);

            foreach (var dir in dirs)
                AddressSuggestions.Add(dir);

            IsAddressSuggestionsOpen = AddressSuggestions.Count > 0;
        }
        catch
        {
            IsAddressSuggestionsOpen = false;
        }
    }

    // --- Recent Folders Persistence ---

    private static string GetRecentFoldersPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "CubicAIExplorer", "recent.json");
    }

    private void LoadRecentFolders()
    {
        try
        {
            var path = GetRecentFoldersPath();
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            var paths = JsonSerializer.Deserialize<List<string>>(json);
            if (paths == null) return;

            foreach (var folderPath in paths.Take(MaxRecentFolders))
            {
                if (string.IsNullOrWhiteSpace(folderPath)) continue;
                var trimmed = folderPath.TrimEnd('\\');
                var name = Path.GetFileName(trimmed);
                if (string.IsNullOrEmpty(name)) name = folderPath;

                RecentFolders.Add(new RecentFolderItem
                {
                    DisplayName = name,
                    FullPath = folderPath
                });
            }
        }
        catch
        {
            // Non-critical — ignore corrupted recent data.
        }
    }

    private void SaveRecentFolders()
    {
        try
        {
            var path = GetRecentFoldersPath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var payload = RecentFolders.Select(static r => r.FullPath).ToList();
            File.WriteAllText(path, JsonSerializer.Serialize(payload));
        }
        catch
        {
            // Non-critical.
        }
    }
}
