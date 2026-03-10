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
}
