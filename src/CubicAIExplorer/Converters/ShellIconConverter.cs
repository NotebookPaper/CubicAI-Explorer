using System.Globalization;
using System.IO;
using System.Windows.Data;
using CubicAIExplorer.Models;
using CubicAIExplorer.Services;
using CubicAIExplorer.ViewModels;

namespace CubicAIExplorer.Converters;

public sealed class ShellIconConverter : IValueConverter
{
    public static IShellIconService? IconService { get; set; }

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (IconService == null)
            return null;

        return value switch
        {
            FileSystemItem item => IconService.GetIcon(item.FullPath, item.ItemType),
            FolderTreeNodeViewModel node => IconService.GetIcon(node.FullPath, GetTreeNodeType(node.FullPath)),
            string path => IconService.GetIcon(path, Directory.Exists(path) ? FileSystemItemType.Directory : FileSystemItemType.File),
            _ => null
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static FileSystemItemType GetTreeNodeType(string path)
    {
        var root = Path.GetPathRoot(path);
        if (!string.IsNullOrWhiteSpace(root) && string.Equals(root, path, StringComparison.OrdinalIgnoreCase))
            return FileSystemItemType.Drive;

        return FileSystemItemType.Directory;
    }
}
