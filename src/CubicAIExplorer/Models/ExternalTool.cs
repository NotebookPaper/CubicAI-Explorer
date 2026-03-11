namespace CubicAIExplorer.Models;

public sealed class ExternalTool
{
    public string Name { get; set; } = string.Empty;
    public string ToolPath { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
}
