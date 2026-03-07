namespace CubicAIExplorer.Services;

public interface IClipboardService
{
    void SetFiles(IEnumerable<string> paths, bool isCut);
    (IReadOnlyList<string> Paths, bool IsCut) GetFiles();
    bool HasFiles();
    void Clear();
}
