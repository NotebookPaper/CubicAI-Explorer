using System.Xml.Linq;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Text.Json;
using System.ComponentModel;
using System.Windows.Input;
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
    private readonly IFileOperationQueueService _fileOperationQueueService;
    private readonly Services.SettingsService? _settingsService;
    private readonly Services.BookmarkService? _bookmarkService;
    private readonly Models.UserSettings _userSettings;

    [ObservableProperty]
    private TabViewModel? _activeTab;

    [ObservableProperty]
    private double _sidebarWidth = 250;

    [ObservableProperty]
    private double _previewWidth = 300;

    [ObservableProperty]
    private FolderTreeNodeViewModel? _selectedTreeNode;

    [ObservableProperty]
    private string _addressBarText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private BookmarkItem? _selectedBookmark;

    [ObservableProperty]
    private SavedSearchItem? _selectedSavedSearch;

    [ObservableProperty]
    private bool _isRightPaneActive;

    // Dual pane
    [ObservableProperty]
    private bool _isDualPaneMode;

    [ObservableProperty]
    private bool _isQueueDetailsVisible;

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

    [ObservableProperty]
    private string _previewStatusText = string.Empty;

    [ObservableProperty]
    private bool _hasPreviewStatus;

    // Address autocomplete
    [ObservableProperty]
    private bool _isAddressSuggestionsOpen;

    [ObservableProperty]
    private bool _isRightPaneSuggestionsOpen;

    // UI Visibility
    [ObservableProperty]
    private bool _isToolbarVisible = true;

    [ObservableProperty]
    private bool _isAddressBarVisible = true;

    [ObservableProperty]
    private bool _isStatusBarVisible = true;

    [ObservableProperty]
    private bool _isDrivesVisible = true;

    [ObservableProperty]
    private bool _isTabsVisible = true;

    [ObservableProperty]
    private bool _isRecentFoldersVisible = true;

    [ObservableProperty]
    private bool _isBookmarksVisible = true;

    [ObservableProperty]
    private bool _isSavedSearchesVisible = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateCurrentNamedSessionCommand))]
    private string _currentNamedSessionName = string.Empty;

    public ObservableCollection<TabViewModel> Tabs { get; } = [];
    public ObservableCollection<FileSystemItem> Drives { get; } = [];
    public ObservableCollection<FolderTreeNodeViewModel> FolderTreeRoots { get; } = [];
    public ObservableCollection<BookmarkItem> Bookmarks { get; } = [];
    public ObservableCollection<NamedSession> NamedSessions { get; } = [];
    public ObservableCollection<BreadcrumbSegment> BreadcrumbSegments { get; } = [];
    public ObservableCollection<RecentFolderItem> RecentFolders { get; } = [];
    public ObservableCollection<SavedSearchItem> SavedSearches { get; } = [];
    public ObservableCollection<string> AddressSuggestions { get; } = [];
    public ObservableCollection<string> RightPaneAddressSuggestions { get; } = [];
    public TabViewModel? CurrentPaneTab => IsRightPaneActive && IsDualPaneMode ? _rightPaneTab : ActiveTab;
    public FileListViewModel? CurrentPaneFileList => CurrentPaneTab?.FileList;
    public string CurrentPanePath => CurrentPaneTab?.CurrentPath ?? string.Empty;
    public bool IsLeftPaneActive => !IsRightPaneActive;
    public string CurrentPaneLabel => IsRightPaneActive && IsDualPaneMode ? "Right Pane" : "Left Pane";
    public string LeftPaneStatusText => ActiveTab?.FileList.StatusText ?? "Ready";
    public string RightPaneStatusText => _rightPaneTab?.FileList.StatusText ?? "Ready";
    public bool IsFileOperationQueueBusy => _fileOperationQueueService.IsBusy;
    public bool CanShowQueueDetails => _fileOperationQueueService.IsBusy
        || _fileOperationQueueService.PendingCount > 0
        || _fileOperationQueueService.HasRecentActivity;
    public string FileOperationQueueStatusText => _fileOperationQueueService.StatusText;
    public string FileOperationQueueCurrentOperationText => _fileOperationQueueService.CurrentOperationText;
    public int FileOperationQueueCurrentOperationCompletedSteps => _fileOperationQueueService.CurrentOperationCompletedSteps;
    public int FileOperationQueueCurrentOperationTotalSteps => _fileOperationQueueService.CurrentOperationTotalSteps;
    public double FileOperationQueueCurrentOperationProgressFraction => _fileOperationQueueService.CurrentOperationProgressFraction;
    public string FileOperationQueueCurrentOperationProgressText => _fileOperationQueueService.CurrentOperationProgressText;
    public string FileOperationQueueCurrentOperationDetailText => _fileOperationQueueService.CurrentOperationDetailText;
    public int FileOperationQueuePendingCount => _fileOperationQueueService.PendingCount;
    public string FileOperationQueueLastCompletedOperationText => _fileOperationQueueService.LastCompletedOperationText;
    public string FileOperationQueueLastCompletedStatusText => _fileOperationQueueService.LastCompletedStatusText;
    public bool CanCancelFileOperationQueue => _fileOperationQueueService.CanCancel;
    public string ActiveUndoDescription => CurrentPaneFileList?.UndoDescription ?? "Undo";
    public string ActiveRedoDescription => CurrentPaneFileList?.RedoDescription ?? "Redo";

    public event EventHandler? DualPaneModeChanged;
    public event EventHandler? PreviewModeChanged;
    public event EventHandler? OpenPreferencesRequested;
    public event EventHandler<FileSystemItem>? BookmarkPropertiesRequested;

    public Models.UserSettings CurrentSettings => _userSettings;
    public IReadOnlyList<DetailsColumnSetting> DetailsColumnSettings => _userSettings.DetailsColumns;

    private const int MaxRecentFolders = 15;

    public MainViewModel(IFileSystemService fileSystemService, IClipboardService clipboardService,
        Services.SettingsService? settingsService = null,
        Models.UserSettings? userSettings = null,
        IFileOperationQueueService? fileOperationQueueService = null,
        Services.BookmarkService? bookmarkService = null)
    {
        _fileSystemService = fileSystemService;
        _clipboardService = clipboardService;
        _fileOperationQueueService = fileOperationQueueService ?? new FileOperationQueueService();
        _settingsService = settingsService;
        _bookmarkService = bookmarkService;
        _userSettings = userSettings ?? new Models.UserSettings();

        // Initialize UI visibility from settings
        _isToolbarVisible = _userSettings.ShowToolbar;
        _isAddressBarVisible = _userSettings.ShowAddressBar;
        _isStatusBarVisible = _userSettings.ShowStatusBar;
        _isDrivesVisible = _userSettings.ShowDrives;
        _isTabsVisible = _userSettings.ShowTabs;
        _isRecentFoldersVisible = _userSettings.ShowRecentFolders;
        _isBookmarksVisible = _userSettings.ShowBookmarks;
        _isSavedSearchesVisible = _userSettings.ShowSavedSearches;

        // Initialize Window state
        _sidebarWidth = _userSettings.SidebarWidth;
        _previewWidth = _userSettings.PreviewWidth;
        RefreshNamedSessionsFromSettings();

        _fileOperationQueueService.PropertyChanged += OnFileOperationQueuePropertyChanged;

        if (_settingsService != null)
            _settingsService.SettingsChanged += OnExternalSettingsChanged;

        if (_bookmarkService != null)
            _bookmarkService.BookmarksChanged += OnExternalBookmarksChanged;

        LoadBookmarks();
        LoadRecentFolders();
        LoadSavedSearches();
        LoadDrives();

        InitializeTabsFromSettings();
    }

    private void InitializeTabsFromSettings()
    {
        var startupSession = GetStartupNamedSession();
        if (startupSession != null && ApplyNamedSession(startupSession, setCurrentSession: true))
            return;

        if (_userSettings.OpenTabs is { Count: > 0 })
        {
            foreach (var path in _userSettings.OpenTabs)
            {
                if (string.IsNullOrWhiteSpace(path) || !_fileSystemService.DirectoryExists(path)) continue;
                var tab = new TabViewModel(_fileSystemService, _clipboardService, _fileOperationQueueService);
                AttachTab(tab);
                Tabs.Add(tab);
                tab.NavigateTo(path);
            }
        }

        if (Tabs.Count == 0)
        {
            NewTab();
        }
        else
        {
            var index = Math.Clamp(_userSettings.ActiveTabIndex, 0, Tabs.Count - 1);
            ActiveTab = Tabs[index];
        }

        if (!string.IsNullOrWhiteSpace(_userSettings.RightPanePath) && _fileSystemService.DirectoryExists(_userSettings.RightPanePath))
        {
            if (!IsDualPaneMode) ToggleDualPane();
            _rightPaneTab?.NavigateTo(_userSettings.RightPanePath);
        }
    }

    private void OnExternalSettingsChanged(object? sender, Models.UserSettings newSettings)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            // Update visibility and window state only - don't force-reload tabs while user is working
            IsToolbarVisible = newSettings.ShowToolbar;
            IsAddressBarVisible = newSettings.ShowAddressBar;
            IsStatusBarVisible = newSettings.ShowStatusBar;
            IsDrivesVisible = newSettings.ShowDrives;
            IsTabsVisible = newSettings.ShowTabs;
            IsRecentFoldersVisible = newSettings.ShowRecentFolders;
            IsBookmarksVisible = newSettings.ShowBookmarks;
            IsSavedSearchesVisible = newSettings.ShowSavedSearches;
            SidebarWidth = newSettings.SidebarWidth;
            PreviewWidth = newSettings.PreviewWidth;
            _userSettings.NamedSessions = newSettings.NamedSessions ?? [];
            _userSettings.StartupSessionName = newSettings.StartupSessionName ?? string.Empty;
            _userSettings.FilterMatchMode = newSettings.FilterMatchMode;
            _userSettings.SearchMatchMode = newSettings.SearchMatchMode;
            _userSettings.ClearFilterOnFolderChange = newSettings.ClearFilterOnFolderChange;
            _userSettings.FilterHistory = newSettings.FilterHistory ?? [];
            _userSettings.DetailsColumns = CloneDetailsColumnSettings(newSettings.DetailsColumns);
            RefreshNamedSessionsFromSettings();
            ApplyFilterPreferencesToAllTabs();
            OnPropertyChanged(nameof(DetailsColumnSettings));
            if (!string.IsNullOrWhiteSpace(CurrentNamedSessionName)
                && FindNamedSession(CurrentNamedSessionName) == null)
            {
                CurrentNamedSessionName = string.Empty;
            }
        });
    }

    private void RefreshNamedSessionsFromSettings()
    {
        NamedSessions.Clear();
        foreach (var session in (_userSettings.NamedSessions ?? [])
                     .Select(CloneNamedSession)
                     .Where(static session => !string.IsNullOrWhiteSpace(session.Name))
                     .OrderBy(static session => session.Name, StringComparer.OrdinalIgnoreCase))
        {
            NamedSessions.Add(session);
        }
    }

    private NamedSession? GetStartupNamedSession()
    {
        if (string.IsNullOrWhiteSpace(_userSettings.StartupSessionName))
            return null;

        return FindNamedSession(_userSettings.StartupSessionName);
    }

    private NamedSession? FindNamedSession(string? rawName)
    {
        var name = NormalizeSessionName(rawName);
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return NamedSessions.FirstOrDefault(session =>
            string.Equals(session.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeSessionName(string? rawName)
        => rawName?.Trim() ?? string.Empty;

    private static NamedSession CloneNamedSession(NamedSession session)
    {
        return new NamedSession
        {
            Name = NormalizeSessionName(session.Name),
            OpenTabs = session.OpenTabs
                .Where(static path => !string.IsNullOrWhiteSpace(path))
                .ToList(),
            ActiveTabIndex = session.ActiveTabIndex,
            RightPanePath = session.RightPanePath ?? string.Empty,
            IsDualPaneMode = session.IsDualPaneMode
        };
    }

    private NamedSession CaptureCurrentSession(string sessionName)
    {
        var openTabs = Tabs
            .Select(static tab => tab.CurrentPath)
            .Where(path => !string.IsNullOrWhiteSpace(path) && _fileSystemService.DirectoryExists(path))
            .ToList();

        return new NamedSession
        {
            Name = NormalizeSessionName(sessionName),
            OpenTabs = openTabs,
            ActiveTabIndex = ActiveTab != null ? Math.Max(0, Tabs.IndexOf(ActiveTab)) : 0,
            RightPanePath = IsDualPaneMode ? _rightPaneTab?.CurrentPath ?? string.Empty : string.Empty,
            IsDualPaneMode = IsDualPaneMode
        };
    }

    private bool ApplyNamedSession(NamedSession session, bool setCurrentSession)
    {
        var tabPaths = session.OpenTabs
            .Where(path => !string.IsNullOrWhiteSpace(path) && _fileSystemService.DirectoryExists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (tabPaths.Count == 0)
            return false;

        foreach (var tab in Tabs.ToList())
        {
            DetachTab(tab);
        }

        Tabs.Clear();
        ActiveTab = null;

        foreach (var path in tabPaths)
        {
            var tab = new TabViewModel(_fileSystemService, _clipboardService, _fileOperationQueueService);
            AttachTab(tab);
            Tabs.Add(tab);
            tab.NavigateTo(path);
            ApplyTabDefaults(tab);
        }

        ActiveTab = Tabs[Math.Clamp(session.ActiveTabIndex, 0, Tabs.Count - 1)];

        if (session.IsDualPaneMode)
        {
            if (!IsDualPaneMode)
                ToggleDualPane();

            var rightPanePath = !string.IsNullOrWhiteSpace(session.RightPanePath)
                && _fileSystemService.DirectoryExists(session.RightPanePath)
                ? session.RightPanePath
                : ActiveTab.CurrentPath;
            _rightPaneTab?.NavigateTo(rightPanePath);
        }
        else if (IsDualPaneMode)
        {
            ToggleDualPane();
        }

        if (setCurrentSession)
            CurrentNamedSessionName = session.Name;

        RefreshCurrentPaneState();
        SaveSettings();
        return true;
    }

    private void ApplyTabDefaults(TabViewModel tab)
    {
        if (_userSettings.ShowHiddenFiles)
            tab.FileList.ShowHiddenFiles = true;
        if (_userSettings.DefaultViewMode is "List" or "Tiles")
            tab.FileList.ViewMode = _userSettings.DefaultViewMode;
    }

    public bool SaveNamedSession(string rawName, bool overwriteExisting)
    {
        var sessionName = NormalizeSessionName(rawName);
        if (string.IsNullOrWhiteSpace(sessionName))
            return false;

        var session = CaptureCurrentSession(sessionName);
        var existing = FindNamedSession(sessionName);

        if (existing != null && !overwriteExisting)
            return false;

        if (existing != null)
        {
            var existingIndex = NamedSessions.IndexOf(existing);
            NamedSessions[existingIndex] = session;
        }
        else
        {
            NamedSessions.Add(session);
        }

        ReorderNamedSessions();
        CurrentNamedSessionName = sessionName;
        SaveSettings();
        return true;
    }

    public bool LoadNamedSession(string rawName)
    {
        var session = FindNamedSession(rawName);
        return session != null && ApplyNamedSession(session, setCurrentSession: true);
    }

    public bool DeleteNamedSession(string rawName)
    {
        var session = FindNamedSession(rawName);
        if (session == null)
            return false;

        NamedSessions.Remove(session);
        if (string.Equals(CurrentNamedSessionName, session.Name, StringComparison.OrdinalIgnoreCase))
            CurrentNamedSessionName = string.Empty;
        if (string.Equals(_userSettings.StartupSessionName, session.Name, StringComparison.OrdinalIgnoreCase))
            _userSettings.StartupSessionName = string.Empty;

        SaveSettings();
        return true;
    }

    public bool SetStartupSession(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            _userSettings.StartupSessionName = string.Empty;
            SaveSettings();
            return true;
        }

        var session = FindNamedSession(rawName);
        if (session == null)
            return false;

        _userSettings.StartupSessionName = session.Name;
        SaveSettings();
        return true;
    }

    public string GetStartupSessionName() => _userSettings.StartupSessionName;

    private void ReorderNamedSessions()
    {
        var ordered = NamedSessions
            .OrderBy(static session => session.Name, StringComparer.OrdinalIgnoreCase)
            .Select(CloneNamedSession)
            .ToList();

        NamedSessions.Clear();
        foreach (var session in ordered)
            NamedSessions.Add(session);
    }

    public void ActivateLeftPane() => IsRightPaneActive = false;

    public void ActivateRightPane()
    {
        if (IsDualPaneMode && _rightPaneTab != null)
            IsRightPaneActive = true;
    }

    private void AttachTab(TabViewModel tab)
    {
        ApplyFilterPreferences(tab.FileList);
        tab.FileList.FilterHistoryEntryAdded += OnFilterHistoryEntryAdded;
        tab.PropertyChanged += OnTabPropertyChanged;
        tab.FileList.PropertyChanged += OnFileListPropertyChanged;
    }

    private void DetachTab(TabViewModel tab)
    {
        tab.FileList.FilterHistoryEntryAdded -= OnFilterHistoryEntryAdded;
        tab.PropertyChanged -= OnTabPropertyChanged;
        tab.FileList.PropertyChanged -= OnFileListPropertyChanged;
    }

    private void OnTabPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not TabViewModel tab)
            return;

        if (e.PropertyName != nameof(TabViewModel.CurrentPath))
            return;

        if (tab == ActiveTab)
            AddToRecentFolders(tab.CurrentPath);

        if (tab == _rightPaneTab)
            RightPaneAddressText = tab.CurrentPath;

        if (tab == CurrentPaneTab)
            RefreshCurrentPaneState();
    }

    private void OnFileListPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not FileListViewModel fileList)
            return;

        RaisePaneStatusProperties(fileList);

        var isCurrentPane = CurrentPaneFileList == fileList;
        if (!isCurrentPane)
            return;

        switch (e.PropertyName)
        {
            case nameof(FileListViewModel.StatusText):
                StatusText = fileList.StatusText;
                RaiseCurrentPaneCommandProperties();
                break;
            case nameof(FileListViewModel.FilterMatchMode):
                UpdateFilterPreferences(fileList.FilterMatchMode, fileList.SearchMatchMode, fileList.ClearFilterOnFolderChange);
                break;
            case nameof(FileListViewModel.SearchMatchMode):
                UpdateFilterPreferences(fileList.FilterMatchMode, fileList.SearchMatchMode, fileList.ClearFilterOnFolderChange);
                break;
            case nameof(FileListViewModel.ClearFilterOnFolderChange):
                UpdateFilterPreferences(fileList.FilterMatchMode, fileList.SearchMatchMode, fileList.ClearFilterOnFolderChange);
                break;
            case nameof(FileListViewModel.SelectedItem):
            case nameof(FileListViewModel.CurrentPath):
                UpdatePreview();
                break;
            case nameof(FileListViewModel.UndoDescription):
            case nameof(FileListViewModel.RedoDescription):
                RaiseCurrentPaneCommandProperties();
                break;
        }
    }

    private void OnFileOperationQueuePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IFileOperationQueueService.IsBusy))
            OnPropertyChanged(nameof(IsFileOperationQueueBusy));

        if (e.PropertyName == nameof(IFileOperationQueueService.StatusText)
            || e.PropertyName == nameof(IFileOperationQueueService.IsBusy)
            || e.PropertyName == nameof(IFileOperationQueueService.HasRecentActivity)
            || e.PropertyName == nameof(IFileOperationQueueService.PendingCount)
            || e.PropertyName == nameof(IFileOperationQueueService.CanCancel)
            || e.PropertyName == nameof(IFileOperationQueueService.CurrentOperationText)
            || e.PropertyName == nameof(IFileOperationQueueService.CurrentOperationCompletedSteps)
            || e.PropertyName == nameof(IFileOperationQueueService.CurrentOperationTotalSteps)
            || e.PropertyName == nameof(IFileOperationQueueService.CurrentOperationDetailText)
            || e.PropertyName == nameof(IFileOperationQueueService.LastCompletedOperationText)
            || e.PropertyName == nameof(IFileOperationQueueService.LastCompletedStatusText))
        {
            OnPropertyChanged(nameof(FileOperationQueueStatusText));
            OnPropertyChanged(nameof(CanShowQueueDetails));
            OnPropertyChanged(nameof(CanCancelFileOperationQueue));
            OnPropertyChanged(nameof(FileOperationQueueCurrentOperationText));
            OnPropertyChanged(nameof(FileOperationQueueCurrentOperationCompletedSteps));
            OnPropertyChanged(nameof(FileOperationQueueCurrentOperationTotalSteps));
            OnPropertyChanged(nameof(FileOperationQueueCurrentOperationProgressFraction));
            OnPropertyChanged(nameof(FileOperationQueueCurrentOperationProgressText));
            OnPropertyChanged(nameof(FileOperationQueueCurrentOperationDetailText));
            OnPropertyChanged(nameof(FileOperationQueuePendingCount));
            OnPropertyChanged(nameof(FileOperationQueueLastCompletedOperationText));
            OnPropertyChanged(nameof(FileOperationQueueLastCompletedStatusText));
        }
    }

    private void OnFilterHistoryEntryAdded(object? sender, string entry)
    {
        var normalized = entry.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return;

        var history = GetFilterHistorySnapshot();
        history.RemoveAll(existing => string.Equals(existing, normalized, StringComparison.OrdinalIgnoreCase));
        history.Insert(0, normalized);
        while (history.Count > 15)
            history.RemoveAt(history.Count - 1);

        _userSettings.FilterHistory = history;
        ApplyFilterHistoryToAllTabs(history);
        SaveSettings();
    }

    private void ApplyFilterPreferences(FileListViewModel fileList)
    {
        fileList.FilterMatchMode = _userSettings.FilterMatchMode;
        fileList.SearchMatchMode = _userSettings.SearchMatchMode;
        fileList.ClearFilterOnFolderChange = _userSettings.ClearFilterOnFolderChange;
        fileList.SetFilterHistory(_userSettings.FilterHistory ?? []);
    }

    private void ApplyFilterPreferencesToAllTabs()
    {
        foreach (var tab in Tabs)
            ApplyFilterPreferences(tab.FileList);

        if (_rightPaneTab != null)
            ApplyFilterPreferences(_rightPaneTab.FileList);
    }

    private void ApplyFilterHistoryToAllTabs(IReadOnlyCollection<string> history)
    {
        foreach (var tab in Tabs)
            tab.FileList.SetFilterHistory(history);

        _rightPaneTab?.FileList.SetFilterHistory(history);
    }

    public IReadOnlyList<DetailsColumnSetting> GetDetailsColumnSettings()
    {
        var settings = CloneDetailsColumnSettings(_userSettings.DetailsColumns);
        if (settings.Count == 0)
            settings = CreateDefaultDetailsColumnSettings();

        return settings
            .OrderBy(static setting => setting.DisplayOrder)
            .ToList();
    }

    public void SaveDetailsColumnSettings(IEnumerable<DetailsColumnSetting> settings)
    {
        _userSettings.DetailsColumns = NormalizeDetailsColumnSettings(settings);
        OnPropertyChanged(nameof(DetailsColumnSettings));
        SaveSettings();
    }

    private static List<DetailsColumnSetting> CreateDefaultDetailsColumnSettings()
    {
        return
        [
            new DetailsColumnSetting { ColumnId = DetailsColumnId.Name, Width = 350, IsVisible = true, DisplayOrder = 0 },
            new DetailsColumnSetting { ColumnId = DetailsColumnId.Size, Width = 100, IsVisible = true, DisplayOrder = 1 },
            new DetailsColumnSetting { ColumnId = DetailsColumnId.Type, Width = 120, IsVisible = true, DisplayOrder = 2 },
            new DetailsColumnSetting { ColumnId = DetailsColumnId.DateModified, Width = 160, IsVisible = true, DisplayOrder = 3 }
        ];
    }

    private static List<DetailsColumnSetting> CloneDetailsColumnSettings(IEnumerable<DetailsColumnSetting>? settings)
    {
        return (settings ?? [])
            .Select(static setting => new DetailsColumnSetting
            {
                ColumnId = setting.ColumnId,
                Width = setting.Width,
                IsVisible = setting.IsVisible,
                DisplayOrder = setting.DisplayOrder
            })
            .ToList();
    }

    private static List<DetailsColumnSetting> NormalizeDetailsColumnSettings(IEnumerable<DetailsColumnSetting>? settings)
    {
        var incoming = CloneDetailsColumnSettings(settings);
        var defaults = CreateDefaultDetailsColumnSettings();
        var byId = incoming
            .GroupBy(static setting => setting.ColumnId)
            .ToDictionary(static group => group.Key, static group => group.First());

        var normalized = new List<DetailsColumnSetting>(defaults.Count);
        for (var i = 0; i < defaults.Count; i++)
        {
            var fallback = defaults[i];
            if (!byId.TryGetValue(fallback.ColumnId, out var candidate))
                candidate = fallback;

            normalized.Add(new DetailsColumnSetting
            {
                ColumnId = fallback.ColumnId,
                Width = candidate.Width > 24 ? candidate.Width : fallback.Width,
                IsVisible = candidate.IsVisible,
                DisplayOrder = candidate.DisplayOrder
            });
        }

        var ordered = normalized
            .OrderBy(static setting => setting.DisplayOrder)
            .ThenBy(static setting => setting.ColumnId)
            .ToList();

        for (var i = 0; i < ordered.Count; i++)
            ordered[i].DisplayOrder = i;

        return ordered;
    }

    private void UpdateFilterPreferences(NameMatchMode filterMatchMode, NameMatchMode searchMatchMode, bool clearFilterOnFolderChange)
    {
        var changed = _userSettings.FilterMatchMode != filterMatchMode
            || _userSettings.SearchMatchMode != searchMatchMode
            || _userSettings.ClearFilterOnFolderChange != clearFilterOnFolderChange;
        if (!changed)
            return;

        _userSettings.FilterMatchMode = filterMatchMode;
        _userSettings.SearchMatchMode = searchMatchMode;
        _userSettings.ClearFilterOnFolderChange = clearFilterOnFolderChange;
        ApplyFilterPreferencesToAllTabs();
        SaveSettings();
    }

    private List<string> GetFilterHistorySnapshot()
    {
        if (_rightPaneTab?.FileList.FilterHistory.Count > 0)
            return _rightPaneTab.FileList.FilterHistory.ToList();

        var source = Tabs.Select(static tab => tab.FileList)
            .FirstOrDefault(static fileList => fileList.FilterHistory.Count > 0)
            ?? CurrentPaneFileList;

        return source?.FilterHistory.ToList() ?? (_userSettings.FilterHistory ?? []);
    }

    private void RefreshCurrentPaneState()
    {
        var currentPaneTab = CurrentPaneTab;
        var path = currentPaneTab?.CurrentPath ?? string.Empty;
        AddressBarText = path;
        StatusText = currentPaneTab?.FileList.StatusText ?? "Ready";
        UpdateBreadcrumbs(path);
        UpdatePreview();
        RaiseCurrentPaneStateProperties();
        
        if (!string.IsNullOrEmpty(path))
        {
            SyncFolderTreeToPath(path);
        }
    }

    public event EventHandler<FolderTreeNodeViewModel>? ScrollToSelectedRequested;
    public event EventHandler<BookmarkItem>? ScrollToSelectedBookmarkRequested;

    private bool _isSyncingTree;

    public void AddBookmarkFromPath(string path, BookmarkItem? targetParent)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        var newItem = new BookmarkItem
        {
            Name = GetDisplayName(path),
            Path = path,
            IsFolder = true,
            IsExpanded = true
        };

        if (targetParent != null && targetParent.IsFolder)
        {
            targetParent.Children.Add(newItem);
            targetParent.IsExpanded = true;
        }
        else
        {
            Bookmarks.Add(newItem);
        }

        SelectedBookmark = newItem;
        SaveBookmarks();
        ScrollToSelectedBookmarkRequested?.Invoke(this, newItem);
    }

    private void SyncFolderTreeToPath(string path)
    {
        if (_isSyncingTree || string.IsNullOrEmpty(path)) return;
        _isSyncingTree = true;

        try
        {
            // 1. Deselect all nodes first to avoid multiple selections
            foreach (var rootNode in FolderTreeRoots)
            {
                DeselectRecursive(rootNode);
            }

            // 2. Find root drive
            var root = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(root)) return;

            var driveNode = FolderTreeRoots.FirstOrDefault(n => string.Equals(n.FullPath, root, StringComparison.OrdinalIgnoreCase));
            if (driveNode == null) return;

            // 3. Start recursive expansion
            ExpandToPathAsync(driveNode, path);
        }
        finally
        {
            _isSyncingTree = false;
        }
    }

    private void DeselectRecursive(FolderTreeNodeViewModel node)
    {
        node.IsSelected = false;
        foreach (var child in node.Children)
        {
            DeselectRecursive(child);
        }
    }

    private async void ExpandToPathAsync(FolderTreeNodeViewModel node, string targetPath)
    {
        if (string.Equals(node.FullPath.TrimEnd('\\'), targetPath.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
        {
            node.IsSelected = true;
            node.IsExpanded = true; // Ensure the target itself is visible if it has children
            ScrollToSelectedRequested?.Invoke(this, node);
            return;
        }

        if (!targetPath.StartsWith(node.FullPath, StringComparison.OrdinalIgnoreCase))
            return;

        if (!node.IsExpanded)
        {
            node.IsExpanded = true;
            // Give WPF a moment to generate child nodes in the background
            await Task.Delay(50);
        }

        var nextPart = targetPath[node.FullPath.Length..].TrimStart('\\');
        var slashIndex = nextPart.IndexOf('\\');
        var currentPart = slashIndex == -1 ? nextPart : nextPart[..slashIndex];
        var nextPath = Path.Combine(node.FullPath, currentPart);

        // Try to find the child. If not found, maybe it's not loaded yet?
        var child = node.Children
            .ToList()
            .FirstOrDefault(c => string.Equals(c.FullPath.TrimEnd('\\'), nextPath.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));
        
        if (child == null)
        {
            // If we just expanded, it should be there. If not, wait a bit more or retry.
            await Task.Delay(50);
            child = node.Children
                .ToList()
                .FirstOrDefault(c => string.Equals(c.FullPath.TrimEnd('\\'), nextPath.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));
        }

        if (child != null)
        {
            ExpandToPathAsync(child, targetPath);
        }
    }

    private static string GetAppDataPath(string filename)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "CubicAIExplorer", filename);
    }

    private string GetDisplayName(string path) => _fileSystemService.GetDisplayName(path);

    private static string GetBookmarksPath()
    {
        var overridePath = Environment.GetEnvironmentVariable("CUBICAI_BOOKMARKS_PATH");
        if (!string.IsNullOrWhiteSpace(overridePath))
            return overridePath;

        return GetAppDataPath("bookmarks.json");
    }

    private void OnExternalBookmarksChanged(object? sender, List<BookmarkItem> bookmarks)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Bookmarks.Clear();
            foreach (var bookmark in bookmarks)
                Bookmarks.Add(bookmark);
        });
    }

    private void LoadBookmarks()
    {
        if (_bookmarkService == null)
        {
            // Fallback for tests or if service is unavailable
            TryAddBookmark(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), save: false);
            TryAddBookmark(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), save: false);
            TryAddBookmark(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads"), save: false);
            return;
        }

        var bookmarks = _bookmarkService.Load();
        if (bookmarks.Count > 0)
        {
            Bookmarks.Clear();
            foreach (var bookmark in bookmarks)
                Bookmarks.Add(bookmark);
        }
        else
        {
            TryAddBookmark(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), save: false);
            TryAddBookmark(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), save: false);
            TryAddBookmark(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads"), save: false);
            SaveBookmarks();
        }
    }

    private BookmarkItem MapRecordToBookmark(BookmarkRecord record)
    {
        var item = new BookmarkItem
        {
            Name = record.Name,
            Path = record.Path,
            IsFolder = true // All bookmarks should be folders
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
        var children = item.Children.Select(MapBookmarkToRecord).ToList();
        return new BookmarkRecord(item.Name, item.Path, item.IsFolder, children.Count > 0 ? children : null);
    }

    private void LoadDrives()
    {
        Drives.Clear();
        FolderTreeRoots.Clear();
        var drives = _fileSystemService.GetDrives();
        foreach (var drive in drives)
        {
            Drives.Add(drive);
            FolderTreeRoots.Add(FolderTreeNodeViewModel.CreateDriveNode(
                _fileSystemService, drive.Name, drive.FullPath));
        }
    }

    [RelayCommand]
    private void NavigateToDrive(FileSystemItem? drive)
    {
        if (drive != null)
        {
            NavigateCurrentPaneToPath(drive.FullPath);
        }
    }

    [RelayCommand]
    private void NewTab()
    {
        var tab = new TabViewModel(_fileSystemService, _clipboardService, _fileOperationQueueService);
        AttachTab(tab);
        Tabs.Add(tab);
        ActiveTab = tab;
        ApplyTabDefaults(tab);

        // Navigate to startup folder or user profile
        var startupFolder = _userSettings.StartupFolder;
        var defaultPath = !string.IsNullOrWhiteSpace(startupFolder)
            && _fileSystemService.DirectoryExists(startupFolder)
            ? startupFolder
            : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        tab.NavigateTo(defaultPath);
    }

    [RelayCommand]
    private void CloseTab(TabViewModel? tab)
    {
        if (tab == null) return;
        var index = Tabs.IndexOf(tab);
        DetachTab(tab);
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
        var targetTab = CurrentPaneTab;
        if (targetTab == null || string.IsNullOrWhiteSpace(AddressBarText)) return;
        var resolvedPath = _fileSystemService.ResolveDirectoryPath(AddressBarText);
        if (!string.IsNullOrWhiteSpace(resolvedPath))
        {
            targetTab.NavigateTo(resolvedPath);
        }
    }

    [RelayCommand]
    private void NavigateUp()
    {
        var targetTab = CurrentPaneTab;
        if (targetTab == null) return;
        var parent = _fileSystemService.GetParentPath(targetTab.CurrentPath);
        if (parent != targetTab.CurrentPath)
        {
            targetTab.NavigateTo(parent);
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        ExecuteOnCurrentPaneTab(tab => ExecuteCommand(tab.GoBackCommand));
    }

    [RelayCommand]
    private void GoForward()
    {
        ExecuteOnCurrentPaneTab(tab => ExecuteCommand(tab.GoForwardCommand));
    }

    [RelayCommand]
    private void Copy()
    {
        ExecuteCurrentPaneFileListCommand(static fileList => fileList.CopyCommand);
    }

    [RelayCommand]
    private void Cut()
    {
        ExecuteCurrentPaneFileListCommand(static fileList => fileList.CutCommand);
    }

    [RelayCommand]
    private void Paste()
    {
        ExecuteCurrentPaneFileListCommand(static fileList => fileList.PasteCommand);
    }

    [RelayCommand]
    private void OpenSelectedItem()
    {
        ExecuteOnCurrentPaneFileList(fileList =>
        {
            if (fileList.SelectedItem is { } item)
                fileList.OpenItemCommand.Execute(item);
        });
    }

    [RelayCommand]
    private void Delete()
    {
        ExecuteCurrentPaneFileListCommand(static fileList => fileList.DeleteCommand);
    }

    [RelayCommand]
    private void PermanentDelete()
    {
        ExecuteCurrentPaneFileListCommand(static fileList => fileList.PermanentDeleteCommand);
    }

    [RelayCommand]
    private void Refresh()
    {
        ExecuteCurrentPaneFileListCommand(static fileList => fileList.RefreshCommand);
    }

    [RelayCommand]
    private void NewFolder()
    {
        ExecuteCurrentPaneFileListCommand(static fileList => fileList.NewFolderCommand);
    }

    [RelayCommand]
    private void Rename()
    {
        ExecuteCurrentPaneFileListCommand(static fileList => fileList.RenameCommand);
    }

    [RelayCommand]
    private void SelectAll()
    {
        ExecuteCurrentPaneFileListCommand(static fileList => fileList.SelectAllCommand);
    }

    [RelayCommand]
    private void ShowProperties()
    {
        ExecuteCurrentPaneFileListCommand(static fileList => fileList.ShowPropertiesCommand);
    }

    [RelayCommand]
    private void OpenInExplorer()
    {
        var fileList = CurrentPaneFileList;
        if (fileList?.SelectedItems.Count > 0)
        {
            _fileSystemService.RevealInExplorer(fileList.SelectedItems.Select(static item => item.FullPath));
            return;
        }

        if (fileList?.SelectedItems.Count == 0 && fileList?.SelectedItem is { } selectedItem)
        {
            _fileSystemService.RevealInExplorer(selectedItem.FullPath);
            return;
        }

        if (!string.IsNullOrWhiteSpace(CurrentPanePath))
            _fileSystemService.OpenInDefaultApp(CurrentPanePath);
    }

    [RelayCommand]
    private void SearchInFolder()
    {
        ExecuteCurrentPaneFileListCommand(static fileList => fileList.SearchInFolderCommand);
    }

    [RelayCommand]
    private void ExecuteSearch()
    {
        ExecuteCurrentPaneFileListCommand(static fileList => fileList.ExecuteSearchCommand);
    }

    [RelayCommand]
    private void CloseSearch()
    {
        ExecuteCurrentPaneFileListCommand(static fileList => fileList.CloseSearchCommand);
    }

    [RelayCommand]
    private void ClearSearchResults()
    {
        ExecuteCurrentPaneFileListCommand(static fileList => fileList.ClearSearchResultsCommand);
    }

    [RelayCommand]
    private void AddCurrentSearch()
    {
        var fileList = CurrentPaneFileList;
        if (fileList == null || string.IsNullOrWhiteSpace(CurrentPanePath) || string.IsNullOrWhiteSpace(fileList.SearchText))
            return;

        var searchTerm = fileList.SearchText.Trim();
        var name = $"{GetDisplayName(CurrentPanePath)}: {searchTerm}";
        var existing = SavedSearches.FirstOrDefault(saved =>
            string.Equals(saved.SearchPath, CurrentPanePath, StringComparison.OrdinalIgnoreCase)
            && string.Equals(saved.SearchTerm, searchTerm, StringComparison.OrdinalIgnoreCase)
            && saved.MatchMode == fileList.SearchMatchMode);
        if (existing != null)
        {
            SelectedSavedSearch = existing;
            return;
        }

        var item = new SavedSearchItem
        {
            Name = name,
            SearchPath = CurrentPanePath,
            SearchTerm = searchTerm,
            MatchMode = fileList.SearchMatchMode
        };
        SavedSearches.Insert(0, item);
        SelectedSavedSearch = item;
        SaveSavedSearches();
    }

    [RelayCommand]
    private void RemoveSavedSearch(SavedSearchItem? savedSearch)
    {
        if (savedSearch == null)
            return;

        SavedSearches.Remove(savedSearch);
        if (SelectedSavedSearch == savedSearch)
            SelectedSavedSearch = null;
        SaveSavedSearches();
    }

    [RelayCommand]
    private void RenameSavedSearch(SavedSearchItem? savedSearch)
    {
        if (savedSearch == null)
            return;

        var dialog = new Views.RenameDialog(savedSearch.Name);
        if (dialog.ShowDialog() != true)
            return;

        RenameSavedSearch(savedSearch, dialog.EnteredName);
    }

    [RelayCommand]
    private void RunSavedSearch(SavedSearchItem? savedSearch)
    {
        if (savedSearch == null || string.IsNullOrWhiteSpace(savedSearch.SearchPath) || string.IsNullOrWhiteSpace(savedSearch.SearchTerm))
            return;
        if (!_fileSystemService.DirectoryExists(savedSearch.SearchPath))
            return;

        NavigateCurrentPaneToPath(savedSearch.SearchPath);
        CurrentPaneFileList?.ApplySavedSearch(savedSearch.SearchPath, savedSearch.SearchTerm, savedSearch.MatchMode);
    }

    [RelayCommand]
    private void Undo()
    {
        ExecuteCurrentPaneFileListCommand(static fileList => fileList.UndoCommand);
    }

    [RelayCommand]
    private void Redo()
    {
        ExecuteCurrentPaneFileListCommand(static fileList => fileList.RedoCommand);
    }

    [RelayCommand]
    private void ClearHistory()
    {
        ExecuteCurrentPaneFileListCommand(static fileList => fileList.ClearHistoryCommand);
    }

    [RelayCommand]
    private void ToggleQueueDetails()
    {
        if (!CanShowQueueDetails)
        {
            IsQueueDetailsVisible = false;
            return;
        }

        IsQueueDetailsVisible = !IsQueueDetailsVisible;
    }

    [RelayCommand]
    private void CancelFileOperationQueue()
    {
        if (_fileOperationQueueService.CanCancel)
            _fileOperationQueueService.CancelCurrent();
    }

    private void ExecuteOnCurrentPaneTab(Action<TabViewModel> action)
    {
        if (CurrentPaneTab is { } tab)
            action(tab);
    }

    private void ExecuteOnCurrentPaneFileList(Action<FileListViewModel> action)
    {
        if (CurrentPaneFileList is { } fileList)
            action(fileList);
    }

    private void ExecuteCurrentPaneFileListCommand(Func<FileListViewModel, ICommand> commandSelector)
    {
        ExecuteOnCurrentPaneFileList(fileList => ExecuteCommand(commandSelector(fileList)));
    }

    private static void ExecuteCommand(ICommand command, object? parameter = null)
    {
        if (command.CanExecute(parameter))
            command.Execute(parameter);
    }

    private void RaiseCurrentPaneStateProperties()
    {
        OnPropertyChanged(nameof(CurrentPaneTab));
        OnPropertyChanged(nameof(CurrentPaneFileList));
        OnPropertyChanged(nameof(CurrentPanePath));
        OnPropertyChanged(nameof(IsLeftPaneActive));
        OnPropertyChanged(nameof(CurrentPaneLabel));
        OnPropertyChanged(nameof(LeftPaneStatusText));
        OnPropertyChanged(nameof(RightPaneStatusText));
        RaiseCurrentPaneCommandProperties();
    }

    private void RaiseCurrentPaneCommandProperties()
    {
        OnPropertyChanged(nameof(ActiveUndoDescription));
        OnPropertyChanged(nameof(ActiveRedoDescription));
    }

    private void RaisePaneStatusProperties(FileListViewModel fileList)
    {
        if (ActiveTab?.FileList == fileList)
            OnPropertyChanged(nameof(LeftPaneStatusText));
        if (_rightPaneTab?.FileList == fileList)
            OnPropertyChanged(nameof(RightPaneStatusText));
    }

    public void DuplicateTab(TabViewModel sourceTab)
    {
        NewTab();
        if (!string.IsNullOrWhiteSpace(sourceTab.CurrentPath))
            ActiveTab!.NavigateTo(sourceTab.CurrentPath);
    }

    private TabViewModel? FindOpenTab(string? rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
            return null;

        var normalizedPath = rawPath.Trim();
        return Tabs.FirstOrDefault(tab =>
            !string.IsNullOrWhiteSpace(tab.CurrentPath)
            && string.Equals(tab.CurrentPath, normalizedPath, StringComparison.OrdinalIgnoreCase));
    }

    private bool ActivateOpenTab(string? rawPath)
    {
        var existingTab = FindOpenTab(rawPath);
        if (existingTab == null)
            return false;

        ActiveTab = existingTab;
        return true;
    }

    public void CloseTabsToLeft(TabViewModel keepTab)
    {
        var keepIndex = Tabs.IndexOf(keepTab);
        if (keepIndex <= 0)
            return;

        CloseTabSet(Tabs.Take(keepIndex).ToList(), keepTab);
    }

    public void CloseTabsToRight(TabViewModel keepTab)
    {
        var keepIndex = Tabs.IndexOf(keepTab);
        if (keepIndex < 0 || keepIndex >= Tabs.Count - 1)
            return;

        CloseTabSet(Tabs.Skip(keepIndex + 1).ToList(), keepTab);
    }

    public void CloseOtherTabs(TabViewModel keepTab)
    {
        CloseTabSet(Tabs.Where(t => t != keepTab).ToList(), keepTab);
    }

    private void CloseTabSet(IReadOnlyCollection<TabViewModel> tabsToClose, TabViewModel fallbackActiveTab)
    {
        if (tabsToClose.Count == 0)
            return;

        var activeTabWasClosed = ActiveTab != null && tabsToClose.Contains(ActiveTab);
        foreach (var tab in tabsToClose)
        {
            DetachTab(tab);
            Tabs.Remove(tab);
        }

        if (Tabs.Count == 0)
        {
            NewTab();
            return;
        }

        if (activeTabWasClosed || ActiveTab == null)
            ActiveTab = Tabs.Contains(fallbackActiveTab) ? fallbackActiveTab : Tabs[0];
    }

    public void NavigateToPath(string path)
    {
        var resolvedPath = _fileSystemService.ResolveDirectoryPath(path);
        if (string.IsNullOrWhiteSpace(resolvedPath))
            return;

        if (ActiveTab == null) NewTab();
        ActiveTab!.NavigateTo(resolvedPath);
    }

    public void NavigateCurrentPaneToPath(string path)
    {
        var resolvedPath = _fileSystemService.ResolveDirectoryPath(path);
        if (string.IsNullOrWhiteSpace(resolvedPath))
            return;

        if (CurrentPaneTab == null)
        {
            if (ActiveTab == null)
                NewTab();
        }

        CurrentPaneTab?.NavigateTo(resolvedPath);
    }

    partial void OnActiveTabChanged(TabViewModel? value)
    {
        RefreshCurrentPaneState();
    }

    partial void OnIsRightPaneActiveChanged(bool value)
    {
        if (value && (!IsDualPaneMode || _rightPaneTab == null))
        {
            IsRightPaneActive = false;
            return;
        }

        RefreshCurrentPaneState();
    }

    public void SelectTreeNode(FolderTreeNodeViewModel node)
    {
        NavigateCurrentPaneToPath(node.FullPath);
    }

    private void UpdateBreadcrumbs(string path)
    {
        BreadcrumbSegments.Clear();
        if (string.IsNullOrWhiteSpace(path)) return;

        var segments = new List<BreadcrumbSegment>();
        var current = path.TrimEnd('\\');

        while (!string.IsNullOrEmpty(current))
        {
            var name = GetDisplayName(current);

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
        RecentFolders.Insert(0, new RecentFolderItem
        {
            DisplayName = GetDisplayName(path),
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
    private void AddBookmarkFolder(BookmarkItem? parent)
    {
        var newItem = new BookmarkItem
        {
            Name = "New Folder",
            IsFolder = true,
            IsExpanded = true
        };

        if (parent != null && parent.IsFolder)
        {
            parent.Children.Add(newItem);
            parent.IsExpanded = true;
        }
        else
        {
            Bookmarks.Add(newItem);
        }

        SelectedBookmark = newItem;
        SaveBookmarks();
    }

    [RelayCommand]
    private void RenameBookmark(BookmarkItem? bookmark)
    {
        if (bookmark == null) return;

        var dialog = new Views.RenameDialog(bookmark.Name);
        if (dialog.ShowDialog() == true)
        {
            bookmark.Name = dialog.EnteredName;
            SaveBookmarks();
        }
    }

    [RelayCommand]
    private void RemoveBookmark(BookmarkItem? bookmark)
    {
        if (bookmark == null) return;
        
        // Remove from root or from parent children
        if (Bookmarks.Contains(bookmark))
        {
            Bookmarks.Remove(bookmark);
        }
        else
        {
            RemoveFromChildren(Bookmarks, bookmark);
        }

        if (SelectedBookmark == bookmark)
            SelectedBookmark = null;
        SaveBookmarks();
    }

    private bool RemoveFromChildren(IEnumerable<BookmarkItem> collection, BookmarkItem target)
    {
        foreach (var item in collection)
        {
            if (item.Children.Remove(target)) return true;
            if (RemoveFromChildren(item.Children, target)) return true;
        }
        return false;
    }

    public void MoveBookmark(BookmarkItem source, BookmarkItem? target)
    {
        if (source == null) return;

        // 1. Remove from current location
        if (Bookmarks.Contains(source))
        {
            Bookmarks.Remove(source);
        }
        else
        {
            RemoveFromChildren(Bookmarks, source);
        }

        // 2. Add to new location
        if (target == null)
        {
            // Drop on empty area -> move to root
            Bookmarks.Add(source);
        }
        else if (target.IsFolder)
        {
            // Drop on folder -> move inside
            target.Children.Add(source);
            target.IsExpanded = true;
        }
        else
        {
            // Drop on bookmark -> move to same level as bookmark
            var parent = FindParent(Bookmarks, target);
            if (parent != null)
            {
                var index = parent.Children.IndexOf(target);
                parent.Children.Insert(index + 1, source);
            }
            else
            {
                var index = Bookmarks.IndexOf(target);
                if (index >= 0)
                    Bookmarks.Insert(index + 1, source);
                else
                    Bookmarks.Add(source);
            }
        }

        SaveBookmarks();
    }

    private BookmarkItem? FindParent(IEnumerable<BookmarkItem> collection, BookmarkItem target)
    {
        foreach (var item in collection)
        {
            if (item.Children.Contains(target)) return item;
            var parent = FindParent(item.Children, target);
            if (parent != null) return parent;
        }
        return null;
    }

    [RelayCommand]
    private void NavigateBookmark(BookmarkItem? bookmark)
    {
        if (bookmark == null) return;
        if (string.IsNullOrWhiteSpace(bookmark.Path)) return;
        
        if (_fileSystemService.DirectoryExists(bookmark.Path))
        {
            NavigateCurrentPaneToPath(bookmark.Path);
        }
    }

    [RelayCommand]
    private void OpenAllInTabs(BookmarkItem? category)
    {
        if (category == null) return;
        OpenAllRecursive(category);
    }

    private void OpenAllRecursive(BookmarkItem item)
    {
        if (!item.IsFolder && !string.IsNullOrWhiteSpace(item.Path))
        {
            OpenBookmarkInNewTab(item);
        }
        foreach (var child in item.Children)
        {
            OpenAllRecursive(child);
        }
    }

    [RelayCommand]
    private void AddSession(BookmarkItem? parent)
    {
        var sessionFolder = new BookmarkItem
        {
            Name = $"Session - {DateTime.Now:g}",
            IsFolder = true,
            IsExpanded = true
        };

        foreach (var tab in Tabs)
        {
            if (string.IsNullOrWhiteSpace(tab.CurrentPath)) continue;
            sessionFolder.Children.Add(new BookmarkItem
            {
                Name = tab.Title,
                Path = tab.CurrentPath,
                IsFolder = false
            });
        }

        if (parent != null && parent.IsFolder)
            parent.Children.Add(sessionFolder);
        else
            Bookmarks.Add(sessionFolder);

        SaveBookmarks();
    }

    [RelayCommand]
    private void ShowBookmarkProperties(BookmarkItem? bookmark)
    {
        if (bookmark == null || string.IsNullOrWhiteSpace(bookmark.Path)) return;
        var isDirectory = _fileSystemService.DirectoryExists(bookmark.Path);
        var isFile = File.Exists(bookmark.Path);
        if (!isDirectory && !isFile) return;

        var itemType = isDirectory ? FileSystemItemType.Directory : FileSystemItemType.File;

        var item = new FileSystemItem
        {
            Name = bookmark.Name,
            FullPath = bookmark.Path,
            ItemType = itemType,
            Extension = Path.GetExtension(bookmark.Path),
            DateModified = isFile
                ? File.GetLastWriteTime(bookmark.Path)
                : Directory.GetLastWriteTime(bookmark.Path),
            DateCreated = isFile
                ? File.GetCreationTime(bookmark.Path)
                : Directory.GetCreationTime(bookmark.Path),
            Size = isFile ? new FileInfo(bookmark.Path).Length : 0,
            Attributes = File.GetAttributes(bookmark.Path),
            ShellTypeName = ShellFileInfoHelper.TryGetTypeName(bookmark.Path, itemType) ?? string.Empty
        };

        BookmarkPropertiesRequested?.Invoke(this, item);
    }

    [RelayCommand]
    private void RefreshBookmarks()
    {
        LoadBookmarks();
    }

    [RelayCommand]
    private void OpenBookmarkInNewTab(BookmarkItem? bookmark)
    {
        if (bookmark == null || string.IsNullOrWhiteSpace(bookmark.Path)) return;
        if (_fileSystemService.DirectoryExists(bookmark.Path))
        {
            if (ActivateOpenTab(bookmark.Path))
                return;

            var tab = new TabViewModel(_fileSystemService, _clipboardService, _fileOperationQueueService);
            AttachTab(tab);
            Tabs.Add(tab);
            ActiveTab = tab;
            ApplyTabDefaults(tab);
            tab.NavigateTo(bookmark.Path);
        }
    }

    [RelayCommand]
    private void OpenBookmarkInOtherPane(BookmarkItem? bookmark)
    {
        if (bookmark == null || string.IsNullOrWhiteSpace(bookmark.Path)) return;
        if (!IsDualPaneMode) ToggleDualPane();
        
        if (_fileSystemService.DirectoryExists(bookmark.Path))
        {
            if (IsRightPaneActive)
                ActiveTab?.NavigateTo(bookmark.Path);
            else
                _rightPaneTab?.NavigateTo(bookmark.Path);
        }
    }

    [RelayCommand]
    private void ImportBookmarks(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;

        try
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (extension == ".xml")
            {
                ImportCubicExplorerXmlBookmarks(filePath);
            }
            else
            {
                var json = File.ReadAllText(filePath);
                var records = JsonSerializer.Deserialize<List<BookmarkRecord>>(json);
                if (records != null)
                {
                    foreach (var record in records)
                    {
                        TryAddBookmark(record.Path, record.Name, save: false);
                    }
                    SaveBookmarks();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to import bookmarks: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImportCubicExplorerXmlBookmarks(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var root = doc.Root;
        if (root == null) return;

        var count = 0;

        void ProcessNode(XElement element, ObservableCollection<BookmarkItem> targetCollection)
        {
            foreach (var node in element.Elements())
            {
                var localName = node.Name.LocalName;
                var isItem = string.Equals(localName, "item", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(localName, "Bookmark", StringComparison.OrdinalIgnoreCase);
                var isCategory = string.Equals(localName, "category", StringComparison.OrdinalIgnoreCase);

                if (!isItem && !isCategory) continue;

                var name = node.Attribute("Name")?.Value 
                          ?? node.Attribute("name")?.Value 
                          ?? node.Attribute("Title")?.Value 
                          ?? "Untitled";

                var path = node.Attribute("Path")?.Value 
                          ?? node.Attribute("path")?.Value 
                          ?? node.Attribute("Location")?.Value 
                          ?? string.Empty;

                var newItem = new BookmarkItem
                {
                    Name = name,
                    Path = path,
                    IsFolder = true // Bookmarks in CubicExplorer XML are folders
                };

                targetCollection.Add(newItem);
                count++;

                // Recurse into children
                ProcessNode(node, newItem.Children);
            }
        }

        Bookmarks.Clear();
        ProcessNode(root, Bookmarks);

        if (count > 0)
        {
            SaveBookmarks();
            MessageBox.Show($"Imported {count} bookmarks hierarchically.", "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }    private bool TryAddBookmarkWithFeedback(string path, string? displayName = null, bool save = true, bool ignoreExistence = false)
    {
        if (!ignoreExistence && !_fileSystemService.DirectoryExists(path)) return false;
        if (Bookmarks.Any(b => string.Equals(b.Path, path, StringComparison.OrdinalIgnoreCase))) return true; // Already exists

        var name = string.IsNullOrWhiteSpace(displayName)
            ? GetDisplayName(path)
            : displayName;

        Bookmarks.Add(new BookmarkItem
        {
            Name = name,
            Path = path,
            IsFolder = true
        });

        if (save)
            SaveBookmarks();

        return true;
    }

    [RelayCommand]
    private void ExportBookmarks(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        try
        {
            var payload = Bookmarks
                .Select(MapBookmarkToRecord)
                .ToList();

            File.WriteAllText(filePath, JsonSerializer.Serialize(payload, BookmarkJsonOptions));
            MessageBox.Show($"Exported {payload.Count} root bookmarks.", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to export bookmarks: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TryAddBookmark(string path, string? displayName = null, bool save = true)
    {
        if (!_fileSystemService.DirectoryExists(path)) return;
        if (Bookmarks.Any(b => string.Equals(b.Path, path, StringComparison.OrdinalIgnoreCase))) return;

        var name = string.IsNullOrWhiteSpace(displayName)
            ? GetDisplayName(path)
            : displayName;

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
        _bookmarkService?.Save(Bookmarks);
    }

    private sealed record BookmarkRecord(string Name, string Path, bool IsFolder = false, List<BookmarkRecord>? Children = null);
    private sealed class SavedSearchRecord
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public NameMatchMode MatchMode { get; set; } = NameMatchMode.Contains;
    }

    // --- Dual Pane ---

    [RelayCommand]
    private void ToggleDualPane()
    {
        IsDualPaneMode = !IsDualPaneMode;
        if (IsDualPaneMode && _rightPaneTab == null)
        {
            _rightPaneTab = new TabViewModel(_fileSystemService, _clipboardService, _fileOperationQueueService);
            AttachTab(_rightPaneTab);
            OnPropertyChanged(nameof(RightPaneTab));

            if (ActiveTab != null && !string.IsNullOrWhiteSpace(ActiveTab.CurrentPath))
                _rightPaneTab.NavigateTo(ActiveTab.CurrentPath);
        }
        else if (!IsDualPaneMode)
        {
            IsRightPaneActive = false;
        }

        RefreshCurrentPaneState();
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

    [RelayCommand]
    private void OpenPreferences()
    {
        OpenPreferencesRequested?.Invoke(this, EventArgs.Empty);
    }

    private bool CanUpdateCurrentNamedSession() => !string.IsNullOrWhiteSpace(CurrentNamedSessionName);

    [RelayCommand(CanExecute = nameof(CanUpdateCurrentNamedSession))]
    private void UpdateCurrentNamedSession()
    {
        SaveNamedSession(CurrentNamedSessionName, overwriteExisting: true);
    }

    public void ApplyAndSaveSettings(Models.UserSettings newSettings)
    {
        _userSettings.DefaultViewMode = newSettings.DefaultViewMode;
        _userSettings.ShowHiddenFiles = newSettings.ShowHiddenFiles;
        _userSettings.StartupFolder = newSettings.StartupFolder;
        _userSettings.StartInDualPane = newSettings.StartInDualPane;
        _userSettings.StartWithPreview = newSettings.StartWithPreview;
        _userSettings.UseShellContextMenu = newSettings.UseShellContextMenu;
        _userSettings.FilterMatchMode = newSettings.FilterMatchMode;
        _userSettings.SearchMatchMode = newSettings.SearchMatchMode;
        _userSettings.ClearFilterOnFolderChange = newSettings.ClearFilterOnFolderChange;
        _userSettings.FilterHistory = newSettings.FilterHistory ?? [];
        _userSettings.DetailsColumns = NormalizeDetailsColumnSettings(newSettings.DetailsColumns);
        ApplyFilterPreferencesToAllTabs();
        OnPropertyChanged(nameof(DetailsColumnSettings));
        _settingsService?.Save(_userSettings);
    }

    private int _previewGeneration;

    public void UpdatePreview()
    {
        _previewGeneration++;
        var generation = _previewGeneration;

        PreviewFileName = string.Empty;
        PreviewFileInfo = string.Empty;
        PreviewText = string.Empty;
        HasPreviewText = false;
        PreviewImageSource = null;
        HasPreviewImage = false;
        PreviewStatusText = string.Empty;
        HasPreviewStatus = false;

        if (!IsPreviewVisible) return;

        var item = CurrentPaneFileList?.SelectedItem;
        if (item == null)
        {
            PreviewStatusText = "Select a file or folder to preview.";
            HasPreviewStatus = true;
            return;
        }

        PreviewFileName = item.Name;

        if (item.ItemType == FileSystemItemType.Directory)
        {
            PreviewFileInfo = item.TypeDescription;
            LoadFolderPreviewAsync(item.FullPath, generation);
            return;
        }

        PreviewFileInfo = $"{item.TypeDescription}\n{item.DisplaySize}";

        var ext = item.Extension.ToLowerInvariant();

        if (IsImageExtension(ext))
        {
            LoadImagePreviewAsync(item.FullPath, generation);
        }
        else if (IsPdfExtension(ext))
        {
            LoadPdfPreviewAsync(item.FullPath, generation);
        }
        else if (IsArchiveExtension(ext))
        {
            LoadArchivePreviewAsync(item.FullPath, generation);
        }
        else if (IsMediaExtension(ext))
        {
            LoadMediaPreviewAsync(item.FullPath, generation);
        }
        else if (IsTextExtension(ext))
        {
            if (item.Size > 1024 * 1024)
            {
                PreviewStatusText = "Text preview is limited to files up to 1 MB.";
                HasPreviewStatus = true;
                return;
            }
            LoadTextPreviewAsync(item.FullPath, generation);
        }
        else if (item.Size <= 4 * 1024)
        {
            LoadBinaryPreviewAsync(item.FullPath, generation);
        }
        else
        {
            ShowFileMetadata(item.FullPath);
        }
    }

    private async void LoadImagePreviewAsync(string path, int generation)
    {
        try
        {
            var bitmap = await Task.Run(() =>
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path);
                bmp.DecodePixelWidth = 280;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            });
            if (_previewGeneration != generation) return;
            PreviewImageSource = bitmap;
            HasPreviewImage = true;
            ShowImageMetadata(path, bitmap.PixelWidth, bitmap.PixelHeight);
        }
        catch
        {
            if (_previewGeneration != generation) return;
            PreviewStatusText = "Image preview unavailable.";
            HasPreviewStatus = true;
        }
    }

    private async void LoadTextPreviewAsync(string path, int generation)
    {
        try
        {
            var preview = await Task.Run(() => ReadTextPreview(path));
            if (_previewGeneration != generation) return;
            PreviewText = preview.Text;
            HasPreviewText = true;
            PreviewStatusText = BuildTextPreviewStatus(path, preview);
            HasPreviewStatus = true;
        }
        catch
        {
            if (_previewGeneration != generation) return;
            PreviewStatusText = "Text preview unavailable.";
            HasPreviewStatus = true;
        }
    }

    private async void LoadBinaryPreviewAsync(string path, int generation)
    {
        try
        {
            var preview = await Task.Run(() => BuildHexPreview(path));
            if (_previewGeneration != generation) return;
            PreviewText = preview;
            HasPreviewText = true;
            PreviewStatusText = $"Binary preview (hex)\nShowing {new FileInfo(path).Length} byte(s)\n\n{GetBasicFileMetadata(path)}";
            HasPreviewStatus = true;
        }
        catch
        {
            if (_previewGeneration != generation) return;
            ShowFileMetadata(path);
        }
    }

    private async void LoadFolderPreviewAsync(string path, int generation)
    {
        try
        {
            var info = await Task.Run(() =>
            {
                var di = new DirectoryInfo(path);
                int files = 0, folders = 0;
                foreach (var entry in di.EnumerateFileSystemInfos())
                {
                    if (entry is DirectoryInfo) folders++;
                    else files++;
                }
                var created = di.CreationTime.ToString("g");
                var modified = di.LastWriteTime.ToString("g");
                return (files, folders, created, modified);
            });
            if (_previewGeneration != generation) return;
            var parts = new List<string>();
            if (info.folders > 0) parts.Add($"{info.folders} folder{(info.folders != 1 ? "s" : "")}");
            if (info.files > 0) parts.Add($"{info.files} file{(info.files != 1 ? "s" : "")}");
            var summary = parts.Count > 0 ? string.Join(", ", parts) : "Empty folder";
            PreviewStatusText = $"{summary}\n\nCreated: {info.created}\nModified: {info.modified}";
            HasPreviewStatus = true;
        }
        catch
        {
            if (_previewGeneration != generation) return;
            PreviewStatusText = "Folder preview is not available.";
            HasPreviewStatus = true;
        }
    }

    private async void LoadPdfPreviewAsync(string path, int generation)
    {
        try
        {
            var metadata = await Task.Run(() => ReadPdfMetadata(path));
            if (_previewGeneration != generation) return;

            var details = new List<string> { "PDF document" };
            if (!string.IsNullOrWhiteSpace(metadata.Version))
                details.Add($"Version: {metadata.Version}");
            if (metadata.ApproxPageCount > 0)
                details.Add($"Approx. pages: {metadata.ApproxPageCount}");
            if (metadata.IsEncrypted)
                details.Add("Encrypted: Yes");

            PreviewStatusText = string.Join("\n", details) + "\n\n" + GetBasicFileMetadata(path);
            HasPreviewStatus = true;
        }
        catch
        {
            if (_previewGeneration != generation) return;
            ShowFileMetadata(path);
        }
    }

    private async void LoadMediaPreviewAsync(string path, int generation)
    {
        try
        {
            var media = await TryReadMediaMetadataAsync(path);
            if (_previewGeneration != generation) return;

            var details = new List<string> { "Media file" };
            var mediaKind = GetMediaKind(path);
            if (!string.IsNullOrWhiteSpace(mediaKind))
                details.Add(mediaKind);
            if (media.Duration is { } duration && duration > TimeSpan.Zero)
                details.Add($"Duration: {FormatDuration(duration)}");
            if (media.Width > 0 && media.Height > 0)
                details.Add($"Dimensions: {media.Width} x {media.Height}");

            PreviewStatusText = string.Join("\n", details) + "\n\n" + GetBasicFileMetadata(path);
            HasPreviewStatus = true;
        }
        catch
        {
            if (_previewGeneration != generation) return;
            ShowFileMetadata(path);
        }
    }

    private async void LoadArchivePreviewAsync(string path, int generation)
    {
        try
        {
            var entries = await Task.Run(() => _fileSystemService.GetArchiveEntries(path, maxEntries: 8));
            if (_previewGeneration != generation) return;

            var fileCount = entries.Count(static entry => !entry.IsDirectory);
            var folderCount = entries.Count(static entry => entry.IsDirectory);
            var details = new List<string> { "ZIP archive" };
            details.Add($"Entries: {entries.Count}");
            details.Add("Preview: first 8 entries");
            if (folderCount > 0)
                details.Add($"Folders: {folderCount}");
            if (fileCount > 0)
                details.Add($"Files: {fileCount}");

            if (entries.Count > 0)
            {
                PreviewText = string.Join("\n", entries.Select(static entry => entry.FullName));
                HasPreviewText = true;
            }

            PreviewStatusText = string.Join("\n", details) + "\n\n" + GetBasicFileMetadata(path);
            HasPreviewStatus = true;
        }
        catch
        {
            if (_previewGeneration != generation) return;
            ShowFileMetadata(path);
        }
    }

    private void ShowFileMetadata(string path)
    {
        try
        {
            PreviewStatusText = GetBasicFileMetadata(path);
            HasPreviewStatus = true;
        }
        catch
        {
            PreviewStatusText = "No preview available for this file type.";
            HasPreviewStatus = true;
        }
    }

    private void ShowImageMetadata(string path, int width, int height)
    {
        var details = new List<string> { "Image file" };
        if (width > 0 && height > 0)
            details.Add($"Dimensions: {width} x {height}");

        PreviewStatusText = string.Join("\n", details) + "\n\n" + GetBasicFileMetadata(path);
        HasPreviewStatus = true;
    }

    private static string GetBasicFileMetadata(string path)
    {
        var fi = new FileInfo(path);
        var text = $"Created: {fi.CreationTime:g}\nModified: {fi.LastWriteTime:g}\nAccessed: {fi.LastAccessTime:g}";
        if (fi.IsReadOnly)
            text += "\nRead-only";

        return text;
    }

    private static TextPreviewResult ReadTextPreview(string path)
    {
        const int maxLines = 200;
        const int maxCharacters = 64 * 1024;

        using var reader = new StreamReader(path, detectEncodingFromByteOrderMarks: true);
        var lines = new List<string>(capacity: Math.Min(maxLines, 32));
        var totalCharacters = 0;
        var wasTruncated = false;

        while (!reader.EndOfStream && lines.Count < maxLines && totalCharacters < maxCharacters)
        {
            var line = reader.ReadLine() ?? string.Empty;
            lines.Add(line);
            totalCharacters += line.Length + Environment.NewLine.Length;
        }

        if (!reader.EndOfStream)
            wasTruncated = true;

        return new TextPreviewResult(
            string.Join("\n", lines),
            lines.Count,
            totalCharacters,
            wasTruncated,
            reader.CurrentEncoding.WebName);
    }

    private static string BuildTextPreviewStatus(string path, TextPreviewResult preview)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        var details = new List<string> { GetTextPreviewKind(extension) };
        details.Add($"Encoding: {preview.EncodingName}");
        details.Add($"Lines shown: {preview.LineCount}");
        if (preview.WasTruncated)
            details.Add("Preview truncated");

        return string.Join("\n", details) + "\n\n" + GetBasicFileMetadata(path);
    }

    private static string GetTextPreviewKind(string extension) => extension switch
    {
        ".json" => "JSON document",
        ".xml" => "XML document",
        ".md" => "Markdown document",
        ".csv" or ".tsv" => "Delimited text",
        ".cs" or ".xaml" or ".js" or ".ts" or ".py" or ".cpp" or ".h" => "Source code",
        ".log" => "Log file",
        _ => "Text file"
    };

    private static string GetMediaKind(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".mp3" or ".wav" or ".flac" or ".aac" or ".m4a" or ".ogg" or ".wma" => "Audio",
            ".mp4" or ".mkv" or ".avi" or ".mov" or ".wmv" or ".webm" or ".m4v" => "Video",
            _ => string.Empty
        };
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return duration.ToString(@"h\:mm\:ss");
        return duration.ToString(@"m\:ss");
    }

    private static string BuildHexPreview(string path)
    {
        const int bytesPerLine = 16;
        var bytes = File.ReadAllBytes(path);
        var lines = new List<string>();

        for (var offset = 0; offset < bytes.Length; offset += bytesPerLine)
        {
            var lineBytes = bytes.Skip(offset).Take(bytesPerLine).ToArray();
            var hex = string.Join(" ", lineBytes.Select(static b => b.ToString("X2")));
            var ascii = new string(lineBytes
                .Select(static b => b >= 32 && b <= 126 ? (char)b : '.')
                .ToArray());
            lines.Add($"{offset:X4}: {hex.PadRight(bytesPerLine * 3 - 1)}  {ascii}");
        }

        return string.Join("\n", lines);
    }

    private static (string Version, int ApproxPageCount, bool IsEncrypted) ReadPdfMetadata(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        var versionBuffer = new byte[Math.Min(1024, (int)stream.Length)];
        _ = stream.Read(versionBuffer, 0, versionBuffer.Length);
        var header = Encoding.ASCII.GetString(versionBuffer);
        var version = string.Empty;
        var marker = "%PDF-";
        var index = header.IndexOf(marker, StringComparison.Ordinal);
        if (index >= 0)
        {
            var start = index + marker.Length;
            var end = start;
            while (end < header.Length && (char.IsDigit(header[end]) || header[end] == '.'))
                end++;
            version = header[start..end];
        }

        const int tailBytes = 2 * 1024 * 1024;
        var readLength = (int)Math.Min(stream.Length, tailBytes);
        stream.Seek(-readLength, SeekOrigin.End);
        var tailBuffer = new byte[readLength];
        _ = stream.Read(tailBuffer, 0, readLength);
        var tailText = Encoding.ASCII.GetString(tailBuffer);

        var pageCount = CountOccurrences(tailText, "/Type /Page")
            + CountOccurrences(tailText, "/Type/Page");
        var isEncrypted = tailText.Contains("/Encrypt", StringComparison.Ordinal);

        return (version, pageCount, isEncrypted);
    }

    private static int CountOccurrences(string text, string token)
    {
        var count = 0;
        var start = 0;
        while (true)
        {
            var index = text.IndexOf(token, start, StringComparison.Ordinal);
            if (index < 0)
                break;
            count++;
            start = index + token.Length;
        }

        return count;
    }

    private static async Task<(TimeSpan? Duration, int Width, int Height)> TryReadMediaMetadataAsync(string path)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
            return (null, 0, 0);

        var tcs = new TaskCompletionSource<(TimeSpan? Duration, int Width, int Height)>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        await dispatcher.InvokeAsync(() =>
        {
            var player = new MediaPlayer();
            var settled = false;

            void Finish((TimeSpan? Duration, int Width, int Height) result)
            {
                if (settled) return;
                settled = true;
                player.MediaOpened -= MediaOpened;
                player.MediaFailed -= MediaFailed;
                player.Close();
                tcs.TrySetResult(result);
            }

            void MediaOpened(object? _, EventArgs __)
            {
                var duration = player.NaturalDuration.HasTimeSpan
                    ? player.NaturalDuration.TimeSpan
                    : (TimeSpan?)null;
                Finish((duration, player.NaturalVideoWidth, player.NaturalVideoHeight));
            }

            void MediaFailed(object? _, ExceptionEventArgs __)
            {
                Finish((null, 0, 0));
            }

            player.MediaOpened += MediaOpened;
            player.MediaFailed += MediaFailed;
            player.Open(new Uri(path));
        });

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(2500)).ConfigureAwait(false);
        if (completed != tcs.Task)
            return (null, 0, 0);

        return await tcs.Task.ConfigureAwait(false);
    }

    private static bool IsImageExtension(string ext) => ext is
        ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".ico" or
        ".tiff" or ".tif" or ".webp";

    private static bool IsPdfExtension(string ext) => ext is ".pdf";

    private static bool IsArchiveExtension(string ext) => ext is ".zip";

    private static bool IsMediaExtension(string ext) => ext is
        ".mp3" or ".wav" or ".flac" or ".aac" or ".m4a" or ".ogg" or ".wma" or
        ".mp4" or ".mkv" or ".avi" or ".mov" or ".wmv" or ".webm" or ".m4v";

    private static bool IsTextExtension(string ext) => ext is
        ".txt" or ".cs" or ".xml" or ".json" or ".md" or ".log" or
        ".ini" or ".cfg" or ".yaml" or ".yml" or ".html" or ".htm" or
        ".css" or ".js" or ".ts" or ".tsx" or ".jsx" or
        ".py" or ".bat" or ".cmd" or ".ps1" or
        ".xaml" or ".csproj" or ".sln" or ".gitignore" or ".editorconfig" or
        ".csv" or ".tsv" or ".sql" or ".sh" or ".toml" or ".env" or ".h" or
        ".c" or ".cpp" or ".hpp" or ".java" or ".go" or ".rs" or ".rb" or ".php" or
        ".swift" or ".kt" or ".scala" or ".r" or ".m" or ".mm" or
        ".dockerfile" or ".makefile" or ".gradle" or ".properties" or
        ".reg" or ".inf" or ".manifest" or ".targets" or ".props";

    private sealed record TextPreviewResult(
        string Text,
        int LineCount,
        int CharacterCount,
        bool WasTruncated,
        string EncodingName);

    // --- Address Autocomplete ---

    private CancellationTokenSource? _suggestionCts;
    private CancellationTokenSource? _rightPaneSuggestionCts;

    public void UpdateAddressSuggestions()
    {
        _suggestionCts?.Cancel();
        UpdateSuggestionsCore(AddressBarText, AddressSuggestions,
            open => IsAddressSuggestionsOpen = open, ref _suggestionCts);
    }

    public void UpdateRightPaneAddressSuggestions()
    {
        _rightPaneSuggestionCts?.Cancel();
        UpdateSuggestionsCore(RightPaneAddressText, RightPaneAddressSuggestions,
            open => IsRightPaneSuggestionsOpen = open, ref _rightPaneSuggestionCts);
    }

    private void UpdateSuggestionsCore(string text, ObservableCollection<string> suggestions,
        Action<bool> setOpen, ref CancellationTokenSource? cts)
    {
        suggestions.Clear();
        if (string.IsNullOrWhiteSpace(text))
        {
            setOpen(false);
            return;
        }

        // Drive-root completion: "C" → "C:\", "C:" → "C:\"
        if (text.Length <= 2 && char.IsLetter(text[0]) && (text.Length == 1 || text[1] == ':'))
        {
            var driveLetter = char.ToUpperInvariant(text[0]);
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && char.ToUpperInvariant(drive.Name[0]) == driveLetter)
                    suggestions.Add(drive.RootDirectory.FullName);
            }
            if (text.Length == 1)
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady && char.ToUpperInvariant(drive.Name[0]) != driveLetter)
                        suggestions.Add(drive.RootDirectory.FullName);
                }
            }
            setOpen(suggestions.Count > 0);
            return;
        }

        if (!text.Contains('\\') && !text.Contains('/'))
        {
            foreach (var suggestion in GetKnownFolderSuggestions(text))
                suggestions.Add(suggestion);

            if (suggestions.Count > 0)
            {
                setOpen(true);
                return;
            }
        }

        // Debounced async lookup for directory suggestions
        if (Application.Current == null)
        {
            PopulateSuggestions(text, suggestions, setOpen);
            return;
        }

        var newCts = new CancellationTokenSource();
        cts = newCts;
        var uiDispatcher = Application.Current.Dispatcher;
        LoadSuggestionsAsync(text, suggestions, setOpen, uiDispatcher, newCts.Token);
    }

    private async void LoadSuggestionsAsync(string text, ObservableCollection<string> suggestions,
        Action<bool> setOpen, System.Windows.Threading.Dispatcher uiDispatcher, CancellationToken token)
    {
        // Debounce: wait 100ms before querying the filesystem
        await Task.Delay(100, token).ConfigureAwait(false);
        if (token.IsCancellationRequested) return;

        List<string>? results = null;
        try
        {
            var query = ParseSuggestionQuery(text);
            results = await Task.Run(() => GetDirectorySuggestions(query.ParentDirectory, query.Prefix), token);
        }
        catch (OperationCanceledException) { return; }
        catch { /* filesystem error — ignore */ }

        if (token.IsCancellationRequested) return;

        await uiDispatcher.InvokeAsync(() =>
        {
            if (token.IsCancellationRequested) return;

            SetSuggestions(suggestions, setOpen, results);
        });
    }

    private void PopulateSuggestions(string text, ObservableCollection<string> suggestions, Action<bool> setOpen)
    {
        var query = ParseSuggestionQuery(text);
        var results = GetDirectorySuggestions(query.ParentDirectory, query.Prefix);
        SetSuggestions(suggestions, setOpen, results);
    }

    private static (string ParentDirectory, string Prefix) ParseSuggestionQuery(string text)
    {
        if (text.EndsWith('\\') || text.EndsWith('/'))
            return (text, string.Empty);

        return (Path.GetDirectoryName(text) ?? string.Empty, Path.GetFileName(text));
    }

    private List<string> GetKnownFolderSuggestions(string text)
    {
        var normalized = text.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return [];

        var aliases = new[]
        {
            "Desktop",
            "Documents",
            "Downloads",
            "Pictures",
            "Music",
            "Videos",
            "Home",
            "Profile"
        };

        return aliases
            .Where(alias => alias.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
            .Select(alias => _fileSystemService.ResolveDirectoryPath(alias))
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<string> GetDirectorySuggestions(string parentDirectory, string prefix)
    {
        if (!_fileSystemService.DirectoryExists(parentDirectory))
            return [];

        return _fileSystemService.GetSubDirectories(parentDirectory)
            .Where(d => string.IsNullOrEmpty(prefix)
                || d.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Take(15)
            .Select(d => d.FullPath)
            .ToList();
    }

    private static void SetSuggestions(ObservableCollection<string> suggestions, Action<bool> setOpen, List<string>? results)
    {
        suggestions.Clear();
        if (results is { Count: > 0 })
        {
            foreach (var dir in results)
                suggestions.Add(dir);
        }

        setOpen(suggestions.Count > 0);
    }

    // --- Recent Folders Persistence ---

    private static string GetRecentFoldersPath() => GetAppDataPath("recent.json");

    private static string GetSavedSearchesPath()
    {
        var overridePath = Environment.GetEnvironmentVariable("CUBICAI_SAVED_SEARCHES_PATH");
        if (!string.IsNullOrWhiteSpace(overridePath))
            return overridePath;

        return GetAppDataPath("saved-searches.json");
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

                RecentFolders.Add(new RecentFolderItem
                {
                    DisplayName = GetDisplayName(folderPath),
                    FullPath = folderPath
                });
            }
        }
        catch
        {
            // Non-critical — ignore corrupted recent data.
        }
    }

    private async void SaveRecentFolders()
    {
        try
        {
            var filePath = GetRecentFoldersPath();
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var payload = RecentFolders.Select(static r => r.FullPath).ToList();
            var json = JsonSerializer.Serialize(payload);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch
        {
            // Non-critical.
        }
    }

    private void LoadSavedSearches()
    {
        try
        {
            var path = GetSavedSearchesPath();
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            var savedSearches = JsonSerializer.Deserialize<List<SavedSearchRecord>>(json);
            if (savedSearches == null) return;

            foreach (var savedSearch in savedSearches)
            {
                if (string.IsNullOrWhiteSpace(savedSearch.Path) || string.IsNullOrWhiteSpace(savedSearch.Term))
                    continue;

                SavedSearches.Add(new SavedSearchItem
                {
                    Name = string.IsNullOrWhiteSpace(savedSearch.Name) ? savedSearch.Term : savedSearch.Name,
                    SearchPath = savedSearch.Path,
                    SearchTerm = savedSearch.Term,
                    MatchMode = savedSearch.MatchMode
                });
            }
        }
        catch
        {
            // Ignore corrupted saved search persistence.
        }
    }

    private void SaveSavedSearches()
    {
        try
        {
            var path = GetSavedSearchesPath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var payload = SavedSearches
                .Select(static s => new SavedSearchRecord
                {
                    Name = s.Name,
                    Path = s.SearchPath,
                    Term = s.SearchTerm,
                    MatchMode = s.MatchMode
                })
                .ToList();
            File.WriteAllText(path, JsonSerializer.Serialize(payload, BookmarkJsonOptions));
        }
        catch
        {
            // Non-critical.
        }
    }

    public void RenameSavedSearch(SavedSearchItem savedSearch, string newName)
    {
        if (savedSearch == null || string.IsNullOrWhiteSpace(newName))
            return;

        savedSearch.Name = newName.Trim();
        OnPropertyChanged(nameof(SavedSearches));
        SaveSavedSearches();
    }

    partial void OnSidebarWidthChanged(double value) { _userSettings.SidebarWidth = value; SaveSettings(); }
    partial void OnPreviewWidthChanged(double value) { _userSettings.PreviewWidth = value; SaveSettings(); }

    private void SaveSettings()
    {
        if (Tabs.Count > 0)
        {
            _userSettings.OpenTabs = Tabs.Select(t => t.CurrentPath).ToList();
            _userSettings.ActiveTabIndex = ActiveTab != null ? Tabs.IndexOf(ActiveTab) : 0;
        }
        _userSettings.RightPanePath = _rightPaneTab?.CurrentPath ?? string.Empty;
        _userSettings.NamedSessions = NamedSessions.Select(CloneNamedSession).ToList();
        _userSettings.FilterHistory = GetFilterHistorySnapshot();
        _userSettings.DetailsColumns = NormalizeDetailsColumnSettings(_userSettings.DetailsColumns);
        _settingsService?.Save(_userSettings);
    }

    partial void OnIsToolbarVisibleChanged(bool value) { _userSettings.ShowToolbar = value; SaveSettings(); }
    partial void OnIsAddressBarVisibleChanged(bool value) { _userSettings.ShowAddressBar = value; SaveSettings(); }
    partial void OnIsStatusBarVisibleChanged(bool value) { _userSettings.ShowStatusBar = value; SaveSettings(); }
    partial void OnIsDrivesVisibleChanged(bool value) { _userSettings.ShowDrives = value; SaveSettings(); }
    partial void OnIsTabsVisibleChanged(bool value) { _userSettings.ShowTabs = value; SaveSettings(); }
    partial void OnIsRecentFoldersVisibleChanged(bool value) { _userSettings.ShowRecentFolders = value; SaveSettings(); }
    partial void OnIsBookmarksVisibleChanged(bool value) { _userSettings.ShowBookmarks = value; SaveSettings(); }
    partial void OnIsSavedSearchesVisibleChanged(bool value) { _userSettings.ShowSavedSearches = value; SaveSettings(); }
}
