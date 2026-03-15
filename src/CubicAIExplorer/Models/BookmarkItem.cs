using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CubicAIExplorer.Models;

public partial class BookmarkItem : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _path = string.Empty;

    [ObservableProperty]
    private bool _isFolder;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isDropTarget;

    [ObservableProperty]
    private bool _isDropIntoTarget;

    [ObservableProperty]
    private bool _isDropBeforeTarget;

    [ObservableProperty]
    private bool _isDropAfterTarget;

    public ObservableCollection<BookmarkItem> Children { get; } = [];
}
