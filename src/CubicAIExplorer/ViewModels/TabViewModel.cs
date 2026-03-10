using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CubicAIExplorer.Services;

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

    public Guid Id { get; } = Guid.NewGuid();
    public FileListViewModel FileList { get; }

    public TabViewModel(
        IFileSystemService fileSystemService,
        IClipboardService clipboardService,
        IFileOperationQueueService? fileOperationQueueService = null)
    {
        _fileSystemService = fileSystemService;
        _fileOperationQueueService = fileOperationQueueService ?? new FileOperationQueueService();
        FileList = new FileListViewModel(fileSystemService, clipboardService, _fileOperationQueueService);
        FileList.NavigateRequested += (_, path) => NavigateTo(path);
        _navigation.Navigated += (_, path) => OnNavigated(path);
    }

    public void NavigateTo(string path)
    {
        _navigation.NavigateTo(path);
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

        FileList.LoadDirectory(path);
        CanGoBack = _navigation.CanGoBack;
        CanGoForward = _navigation.CanGoForward;
    }
}
