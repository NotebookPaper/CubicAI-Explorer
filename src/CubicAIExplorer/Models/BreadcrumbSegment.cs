using System.Windows;

namespace CubicAIExplorer.Models;

public sealed class BreadcrumbSegment
{
    public required string DisplayName { get; init; }
    public required string FullPath { get; init; }
    public bool IsFirst { get; init; }
    public Visibility SeparatorVisibility => IsFirst ? Visibility.Collapsed : Visibility.Visible;
}
