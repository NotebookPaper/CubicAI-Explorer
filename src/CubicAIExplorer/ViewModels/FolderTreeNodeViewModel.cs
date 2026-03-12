using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CubicAIExplorer.Services;

namespace CubicAIExplorer.ViewModels;

public partial class FolderTreeNodeViewModel : ObservableObject
{
    private readonly IFileSystemService _fileSystemService;
    private bool _hasLoadedChildren;
    private bool _isLoadingChildren;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _fullPath = string.Empty;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isSelected;

    public ObservableCollection<FolderTreeNodeViewModel> Children { get; } = [];

    public FolderTreeNodeViewModel(IFileSystemService fileSystemService)
    {
        _fileSystemService = fileSystemService;
    }

    partial void OnIsExpandedChanged(bool value)
    {
        if (value && !_hasLoadedChildren)
        {
            _ = LoadChildrenAsync();
        }
    }

    public void LoadChildren()
        => LoadChildrenAsync().GetAwaiter().GetResult();

    public async Task LoadChildrenAsync()
    {
        if (_hasLoadedChildren || _isLoadingChildren)
            return;

        _hasLoadedChildren = true;
        _isLoadingChildren = true;
        Children.Clear();
        Children.Add(new FolderTreeNodeViewModel(_fileSystemService) { Name = "Loading..." });

        try
        {
            var subdirs = await Task.Run(() => _fileSystemService.GetSubDirectories(FullPath));
            Children.Clear();

            foreach (var dir in subdirs)
            {
                var child = new FolderTreeNodeViewModel(_fileSystemService)
                {
                    Name = dir.Name,
                    FullPath = dir.FullPath
                };
                child.Children.Add(new FolderTreeNodeViewModel(_fileSystemService) { Name = "Loading..." });
                Children.Add(child);
            }
        }
        finally
        {
            _isLoadingChildren = false;
        }
    }

    public static FolderTreeNodeViewModel CreateDriveNode(IFileSystemService fs, string name, string path)
    {
        var node = new FolderTreeNodeViewModel(fs)
        {
            Name = name,
            FullPath = path
        };
        // Dummy child for expander
        node.Children.Add(new FolderTreeNodeViewModel(fs) { Name = "Loading..." });
        return node;
    }
}
