using System.Windows.Media.Imaging;
using CubicAIExplorer.Models;

namespace CubicAIExplorer.Services;

public interface IShellIconService
{
    BitmapSource? GetIcon(string path, FileSystemItemType itemType, bool smallIcon = true);
}
