using System.Globalization;
using System.Windows.Data;
using CubicAIExplorer.Models;

namespace CubicAIExplorer.Converters;

/// <summary>
/// Converts FileSystemItemType to a unicode icon character for display.
/// A proper icon provider using SHGetFileInfo can be added later.
/// </summary>
public sealed class FileIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FileSystemItemType type)
        {
            return type switch
            {
                FileSystemItemType.Drive => "\uD83D\uDDB4",      // disk
                FileSystemItemType.Directory => "\uD83D\uDCC1",   // folder
                FileSystemItemType.File => "\uD83D\uDCC4",        // page
                _ => "\u2753"
            };
        }
        return "\uD83D\uDCC4";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
