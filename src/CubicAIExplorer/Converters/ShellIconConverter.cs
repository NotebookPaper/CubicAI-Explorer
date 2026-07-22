using System.Globalization;
using System.IO;
using System.Windows.Data;
using CubicAIExplorer.Models;
using CubicAIExplorer.Services;
using CubicAIExplorer.ViewModels;

namespace CubicAIExplorer.Converters;

public sealed class ShellIconConverter : IValueConverter, IMultiValueConverter
{
    public static IShellIconService? IconService { get; set; }

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (IconService == null)
            return null;

        try
        {
            return value switch
            {
                FileSystemItem item => IconService.GetIcon(item.FullPath, item.ItemType),
                FolderTreeNodeViewModel node => IconService.GetIcon(node.FullPath, GetTreeNodeType(node.FullPath)),
                BookmarkItem bookmark => GetBookmarkIcon(bookmark),
                string path => IconService.GetIcon(path, Directory.Exists(path) ? FileSystemItemType.Directory : FileSystemItemType.File),
                _ => null
            };
        }
        catch
        {
            // Rendering a row should not fail because the shell icon lookup failed.
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    /// <summary>
    /// MultiBinding entry point for bookmark icons: values are (path, isFolder,
    /// targetIsFile, isExpanded). Including the cached target-kind flags lets the
    /// icon refresh when background validation resolves them, with zero disk I/O;
    /// the expanded flag switches folders/categories to their open-folder icon,
    /// like the original CubicExplorer did.
    /// </summary>
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (IconService == null || values.Length < 3)
            return null;

        var path = values[0] as string ?? string.Empty;
        var isFolder = values[1] is bool folder && folder;
        var targetIsFile = values[2] is bool file && file;
        var isExpanded = values.Length > 3 && values[3] is bool expanded && expanded;

        try
        {
            var type = ResolveBookmarkType(isFolder, targetIsFile);
            var openFolder = isExpanded && type == FileSystemItemType.Directory;
            return IconService.GetIcon(path, type, openFolder: openFolder);
        }
        catch
        {
            return null;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static object? GetBookmarkIcon(BookmarkItem bookmark)
    {
        // Folder categories always show a folder icon; leaf bookmarks show the
        // real icon of their target (file-type icon for files), like the
        // original CubicExplorer did. The target kind is cached on the model so
        // this stays off the filesystem (a dead UNC share must not block the UI
        // thread on a network timeout at every render).
        return IconService?.GetIcon(bookmark.Path, ResolveBookmarkType(bookmark.IsFolder, bookmark.TargetIsFile));
    }

    private static FileSystemItemType ResolveBookmarkType(bool isFolder, bool targetIsFile)
        => isFolder || !targetIsFile ? FileSystemItemType.Directory : FileSystemItemType.File;

    private static FileSystemItemType GetTreeNodeType(string path)
    {
        var root = Path.GetPathRoot(path);
        if (!string.IsNullOrWhiteSpace(root) && string.Equals(root, path, StringComparison.OrdinalIgnoreCase))
            return FileSystemItemType.Drive;

        return FileSystemItemType.Directory;
    }
}
