using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CubicAIExplorer.Services;
using System.Windows.Media;

namespace CubicAIExplorer.ViewModels;

public partial class TabViewModel : ObservableObject
{
    private readonly NavigationService _navigation = new();
    private readonly IFileSystemService _fileSystemService;
    private readonly IFileOperationQueueService _fileOperationQueueService;

    [ObservableProperty]
    private string _title = "New Tab";

    [ObservableProperty]
    private string _currentPath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoBackCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoForwardCommand))]
    private bool _canGoBack;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoBackCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoForwardCommand))]
    private bool _canGoForward;

    [ObservableProperty]
    private bool _isLocked;

    [ObservableProperty]
    private string _lockedRootPath = string.Empty;

    [ObservableProperty]
    private string _tabColor = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();
    public FileListViewModel FileList { get; }
    public Brush TabHeaderBackground => CreateTabBrush(TabColor, defaultOpacity: 0.22);
    public Brush TabAccentBrush => CreateTabBrush(TabColor, defaultOpacity: 0.9);
    public bool HasTabColor => !string.IsNullOrWhiteSpace(TabColor);
    public event EventHandler<string>? NavigateRequested;

    public TabViewModel(
        IFileSystemService fileSystemService,
        IClipboardService clipboardService,
        IFileOperationQueueService? fileOperationQueueService = null)
    {
        _fileSystemService = fileSystemService;
        _fileOperationQueueService = fileOperationQueueService ?? new FileOperationQueueService();
        FileList = new FileListViewModel(fileSystemService, clipboardService, _fileOperationQueueService);
        FileList.NavigateRequested += (_, path) => NavigateRequested?.Invoke(this, path);
        _navigation.Navigated += (_, path) => OnNavigated(path);
    }

    public void NavigateTo(string path)
    {
        _navigation.NavigateTo(path);
    }

    public string? PeekBackPath() => _navigation.BackPath;

    public string? PeekForwardPath() => _navigation.ForwardPath;

    public bool AllowsPathWithinLock(string path)
    {
        if (!IsLocked)
            return true;

        var root = LockedRootPath?.TrimEnd('\\') ?? string.Empty;
        if (string.IsNullOrWhiteSpace(root))
            return true;

        var candidate = path.TrimEnd('\\');
        if (string.Equals(candidate, root, StringComparison.OrdinalIgnoreCase))
            return true;

        return candidate.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase);
    }

    public void SetLockState(bool isLocked)
    {
        IsLocked = isLocked;
        LockedRootPath = isLocked
            ? (CurrentPath?.TrimEnd('\\') ?? string.Empty)
            : string.Empty;
    }

    public void ApplyPersistedState(Models.TabItem? tabState)
    {
        if (tabState == null)
            return;

        IsLocked = tabState.IsLocked;
        LockedRootPath = tabState.LockedRootPath?.TrimEnd('\\') ?? string.Empty;
        TabColor = tabState.TabColor?.Trim() ?? string.Empty;
    }

    public Models.TabItem ToPersistedItem()
    {
        return new Models.TabItem
        {
            Path = CurrentPath,
            Title = Title,
            IsLocked = IsLocked,
            LockedRootPath = LockedRootPath,
            TabColor = TabColor
        };
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack()
    {
        _navigation.GoBack();
    }

    [RelayCommand(CanExecute = nameof(CanGoForward))]
    private void GoForward()
    {
        _navigation.GoForward();
    }

    private void OnNavigated(string path)
    {
        CurrentPath = path;
        Title = _fileSystemService.GetDisplayName(path);
        if (IsLocked && string.IsNullOrWhiteSpace(LockedRootPath))
            LockedRootPath = path.TrimEnd('\\');

        FileList.LoadDirectory(path);
        CanGoBack = _navigation.CanGoBack;
        CanGoForward = _navigation.CanGoForward;
    }

    partial void OnTabColorChanged(string value)
    {
        OnPropertyChanged(nameof(HasTabColor));
        OnPropertyChanged(nameof(TabHeaderBackground));
        OnPropertyChanged(nameof(TabAccentBrush));
    }

    private static Brush CreateTabBrush(string tabColor, double defaultOpacity)
    {
        if (string.IsNullOrWhiteSpace(tabColor))
            return Brushes.Transparent;

        try
        {
            if (ColorConverter.ConvertFromString(tabColor) is Color color)
            {
                color.A = (byte)Math.Clamp((int)Math.Round(255 * defaultOpacity), 0, 255);
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                return brush;
            }
        }
        catch
        {
        }

        return Brushes.Transparent;
    }
}
