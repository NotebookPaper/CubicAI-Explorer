using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CubicAIExplorer.Services;

namespace CubicAIExplorer.ViewModels;

public partial class FolderTreeNodeViewModel : ObservableObject
{
    private readonly IFileSystemService _fileSystemService;
    private bool _hasLoadedChildren;

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
            LoadChildren();
        }
    }

    public void LoadChildren()
    {
        _hasLoadedChildren = true;
        Children.Clear();

        var subdirs = _fileSystemService.GetSubDirectories(FullPath);
        foreach (var dir in subdirs)
        {
            var child = new FolderTreeNodeViewModel(_fileSystemService)
            {
                Name = dir.Name,
                FullPath = dir.FullPath
            };
            // Add a dummy child so the expander arrow shows
            child.Children.Add(new FolderTreeNodeViewModel(_fileSystemService) { Name = "Loading..." });
            Children.Add(child);
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
