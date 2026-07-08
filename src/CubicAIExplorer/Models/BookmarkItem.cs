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

    /// <summary>
    /// True when the bookmark's target path no longer exists; rendered ghosted
    /// like the original CubicExplorer did for dead bookmarks.
    /// </summary>
    [ObservableProperty]
    private bool _isMissing;

    /// <summary>
    /// Cached kind of a leaf bookmark's target: true when the target is a file,
    /// false when it is a directory (or unknown/missing). Populated on the
    /// background validation pass so icon rendering needs no disk I/O.
    /// </summary>
    [ObservableProperty]
    private bool _targetIsFile;

    public ObservableCollection<BookmarkItem> Children { get; } = [];
}
