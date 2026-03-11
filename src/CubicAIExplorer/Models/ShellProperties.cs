namespace CubicAIExplorer.Models;

public sealed record ShellProperties
{
    public string? Company { get; init; }
    public string? Copyright { get; init; }
    public string? FileVersion { get; init; }
    public string? FileDescription { get; init; }
    public string? Dimensions { get; init; }
    public string? Duration { get; init; }

    public static ShellProperties Empty { get; } = new();

    public bool IsEmpty => 
        Company == null && Copyright == null && FileVersion == null && 
        FileDescription == null && Dimensions == null && Duration == null;
}
