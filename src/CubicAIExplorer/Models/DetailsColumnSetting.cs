namespace CubicAIExplorer.Models;

public sealed class DetailsColumnSetting
{
    public DetailsColumnId ColumnId { get; set; }
    public double Width { get; set; }
    public bool IsVisible { get; set; } = true;
    public int DisplayOrder { get; set; }
}
