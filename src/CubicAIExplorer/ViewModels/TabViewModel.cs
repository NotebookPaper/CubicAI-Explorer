using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CubicAIExplorer.Services;

namespace CubicAIExplorer.ViewModels;

public partial class TabViewModel : ObservableObject
{
    private readonly NavigationService _navigation = new();

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

    public TabViewModel(IFileSystemService fileSystemService)
    {
        FileList = new FileListViewModel(fileSystemService);
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
        Title = System.IO.Path.GetFileName(path);
        if (string.IsNullOrEmpty(Title))
            Title = path; // Drive root like "C:\"

        FileList.LoadDirectory(path);
        CanGoBack = _navigation.CanGoBack;
        CanGoForward = _navigation.CanGoForward;
    }
}
