using System.Collections.ObjectModel;
using System.Windows;

namespace CubicAIExplorer.Models;

public sealed class BreadcrumbSegment
{
    public required string DisplayName { get; init; }
    public required string FullPath { get; init; }
    public bool IsFirst { get; init; }
    public bool IsLast { get; init; }
    public ObservableCollection<BreadcrumbDropdownItem> DropdownItems { get; } = [];
    public Visibility SeparatorVisibility => IsFirst ? Visibility.Collapsed : Visibility.Visible;
    public Visibility DropdownVisibility => IsLast ? Visibility.Collapsed : Visibility.Visible;
}
