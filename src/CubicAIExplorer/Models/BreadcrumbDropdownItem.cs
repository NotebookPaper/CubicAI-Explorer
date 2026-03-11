namespace CubicAIExplorer.Models;

public sealed class BreadcrumbDropdownItem
{
    public required string DisplayName { get; init; }
    public string FullPath { get; init; } = string.Empty;
    public bool IsEnabled { get; init; } = true;
}
