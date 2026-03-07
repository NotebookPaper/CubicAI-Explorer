using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CubicAIExplorer.Services;

namespace CubicAIExplorer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFileSystemService _fileSystemService;

    [ObservableProperty]
    private TabViewModel? _activeTab;

    [ObservableProperty]
    private FolderTreeNodeViewModel? _selectedTreeNode;

    [ObservableProperty]
    private string _addressBarText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    public ObservableCollection<TabViewModel> Tabs { get; } = [];
    public ObservableCollection<FolderTreeNodeViewModel> FolderTreeRoots { get; } = [];

    public MainViewModel(IFileSystemService fileSystemService)
    {
        _fileSystemService = fileSystemService;
        LoadDrives();
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
        var tab = new TabViewModel(_fileSystemService);
        tab.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TabViewModel.CurrentPath) && s == ActiveTab)
            {
                AddressBarText = ((TabViewModel)s!).CurrentPath;
                StatusText = ((TabViewModel)s!).FileList.StatusText;
            }
        };
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
        }
    }

    public void SelectTreeNode(FolderTreeNodeViewModel node)
    {
        NavigateToPath(node.FullPath);
    }
}
