using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CubicAIExplorer.Models;
using CubicAIExplorer.Services;

namespace CubicAIExplorer.ViewModels;

public partial class FolderTreeNodeViewModel : ObservableObject
{
    private readonly IFileSystemService _fileSystemService;
    private readonly Func<bool>? _showHiddenProvider;
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

    [ObservableProperty]
    private bool _isPlaceholder;

    public ObservableCollection<FolderTreeNodeViewModel> Children { get; } = [];

    public FolderTreeNodeViewModel(IFileSystemService fileSystemService, Func<bool>? showHiddenProvider = null)
    {
        _fileSystemService = fileSystemService;
        _showHiddenProvider = showHiddenProvider;
    }

    private bool ShowHidden => _showHiddenProvider?.Invoke() ?? false;

    partial void OnIsExpandedChanged(bool value)
    {
        if (value && !_hasLoadedChildren)
        {
            _ = LoadChildrenAsync();
        }
    }

    public async Task LoadChildrenAsync()
    {
        if (_hasLoadedChildren || _isLoadingChildren)
            return;

        _hasLoadedChildren = true;
        _isLoadingChildren = true;
        Children.Clear();
        Children.Add(CreatePlaceholder(_fileSystemService));

        try
        {
            var subdirs = await Task.Run(LoadChildEntries);
            PopulateChildren(subdirs);
        }
        finally
        {
            _isLoadingChildren = false;
        }
    }

    /// <summary>
    /// Re-reads this node's children from disk (e.g. after the hidden-files
    /// setting changed), preserving already-expanded descendants where possible.
    /// </summary>
    public async Task ReloadChildrenAsync()
    {
        if (!_hasLoadedChildren || _isLoadingChildren)
            return;

        _isLoadingChildren = true;
        try
        {
            var subdirs = await Task.Run(LoadChildEntries);
            var previous = Children.Where(c => !c.IsPlaceholder)
                .ToDictionary(c => c.FullPath, StringComparer.OrdinalIgnoreCase);

            Children.Clear();
            foreach (var (dir, hasSubdirs) in subdirs)
            {
                if (previous.TryGetValue(dir.FullPath, out var existing))
                {
                    Children.Add(existing);
                    if (existing.IsExpanded)
                        _ = existing.ReloadChildrenAsync();
                }
                else
                {
                    Children.Add(CreateChildNode(dir, hasSubdirs));
                }
            }
        }
        finally
        {
            _isLoadingChildren = false;
        }
    }

    public static FolderTreeNodeViewModel CreateDriveNode(IFileSystemService fs, string name, string path,
        Func<bool>? showHiddenProvider = null)
    {
        var node = new FolderTreeNodeViewModel(fs, showHiddenProvider)
        {
            Name = name,
            FullPath = path
        };
        node.Children.Add(CreatePlaceholder(fs));
        return node;
    }

    private static FolderTreeNodeViewModel CreatePlaceholder(IFileSystemService fs)
        => new(fs) { Name = "Loading...", IsPlaceholder = true };

    private List<(FileSystemItem Item, bool HasSubdirs)> LoadChildEntries()
    {
        var showHidden = ShowHidden;
        return _fileSystemService.GetSubDirectories(FullPath, showHidden)
            .Select(dir => (dir, _fileSystemService.HasSubDirectories(dir.FullPath, showHidden)))
            .ToList();
    }

    private FolderTreeNodeViewModel CreateChildNode(FileSystemItem dir, bool hasSubdirs)
    {
        var child = new FolderTreeNodeViewModel(_fileSystemService, _showHiddenProvider)
        {
            Name = dir.Name,
            FullPath = dir.FullPath
        };
        if (hasSubdirs)
            child.Children.Add(CreatePlaceholder(_fileSystemService));
        return child;
    }

    private void PopulateChildren(List<(FileSystemItem Item, bool HasSubdirs)> subdirs)
    {
        Children.Clear();

        foreach (var (dir, hasSubdirs) in subdirs)
        {
            Children.Add(CreateChildNode(dir, hasSubdirs));
        }
    }
}
