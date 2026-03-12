using CommunityToolkit.Mvvm.ComponentModel;

namespace CubicAIExplorer.Models;

public partial class SavedSearchItem : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private string _searchPath = string.Empty;

    [ObservableProperty]
    private NameMatchMode _matchMode = NameMatchMode.Contains;

    [ObservableProperty]
    private bool _includeContent;

    [ObservableProperty]
    private string _contentSearchTerm = string.Empty;

    [ObservableProperty]
    private bool _includeHidden;

    [ObservableProperty]
    private bool _includeSystem;

    [ObservableProperty]
    private bool _readOnlyOnly;

    [ObservableProperty]
    private bool _archiveOnly;

    [ObservableProperty]
    private long? _minSize;

    [ObservableProperty]
    private long? _maxSize;

    [ObservableProperty]
    private DateTime? _minDate;

    [ObservableProperty]
    private DateTime? _maxDate;
}
