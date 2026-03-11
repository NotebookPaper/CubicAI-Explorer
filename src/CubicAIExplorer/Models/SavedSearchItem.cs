using CommunityToolkit.Mvvm.ComponentModel;

namespace CubicAIExplorer.Models;

public partial class SavedSearchItem : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();

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
}
