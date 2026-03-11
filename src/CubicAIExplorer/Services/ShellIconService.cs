using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using CubicAIExplorer.Models;

namespace CubicAIExplorer.Services;

public sealed class ShellIconService : IShellIconService
{
    private const uint SHGFI_ICON = 0x000000100;
    private const uint SHGFI_SMALLICON = 0x000000001;
    private const uint SHGFI_LARGEICON = 0x000000000;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
    private const uint SHGFI_LINKOVERLAY = 0x000008000;
    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

    private readonly ConcurrentDictionary<string, BitmapSource?> _cache = new(StringComparer.OrdinalIgnoreCase);

    public BitmapSource? GetIcon(string path, FileSystemItemType itemType, bool smallIcon = true)
    {
        var cacheKey = BuildCacheKey(path, itemType, smallIcon);
        return _cache.GetOrAdd(cacheKey, _ => LoadIcon(path, itemType, smallIcon));
    }

    private static string BuildCacheKey(string path, FileSystemItemType itemType, bool smallIcon)
    {
        var sizeKey = smallIcon ? "sm" : "lg";
        if (itemType == FileSystemItemType.File)
        {
            var extension = Path.GetExtension(path);
            return $"file:{extension}:{sizeKey}";
        }

        if (itemType == FileSystemItemType.Bookmark)
        {
            return $"bookmark:{path}:{sizeKey}";
        }

        return $"path:{path}:{sizeKey}";
    }

    private static BitmapSource? LoadIcon(string path, FileSystemItemType itemType, bool smallIcon)
    {
        try
        {
            var flags = SHGFI_ICON | (smallIcon ? SHGFI_SMALLICON : SHGFI_LARGEICON);
            var fileAttributes = FILE_ATTRIBUTE_NORMAL;
            var shellPath = path;

            if (itemType == FileSystemItemType.File)
            {
                shellPath = string.IsNullOrWhiteSpace(Path.GetExtension(path)) ? ".txt" : Path.GetExtension(path);
                flags |= SHGFI_USEFILEATTRIBUTES;
            }
            else if (itemType == FileSystemItemType.Directory || itemType == FileSystemItemType.Drive || itemType == FileSystemItemType.Bookmark)
            {
                fileAttributes = FILE_ATTRIBUTE_DIRECTORY;

                if (itemType == FileSystemItemType.Bookmark)
                    flags |= SHGFI_LINKOVERLAY;

                if (!Directory.Exists(path) || itemType == FileSystemItemType.Bookmark)
                {
                    flags |= SHGFI_USEFILEATTRIBUTES;
                    shellPath = "C:\\dummy_folder";
                }
            }

            var fileInfo = new SHFILEINFO();
            var result = SHGetFileInfo(
                shellPath,
                fileAttributes,
                ref fileInfo,
                (uint)Marshal.SizeOf<SHFILEINFO>(),
                flags);

            if ((result == IntPtr.Zero || fileInfo.hIcon == IntPtr.Zero) && (flags & SHGFI_USEFILEATTRIBUTES) == 0)
            {
                flags |= SHGFI_USEFILEATTRIBUTES;
                result = SHGetFileInfo(
                    "C:\\dummy_folder",
                    FILE_ATTRIBUTE_DIRECTORY,
                    ref fileInfo,
                    (uint)Marshal.SizeOf<SHFILEINFO>(),
                    flags);
            }

            if (result == IntPtr.Zero || fileInfo.hIcon == IntPtr.Zero)
                return null;

            try
            {
                var image = Imaging.CreateBitmapSourceFromHIcon(
                    fileInfo.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                image.Freeze();
                return image;
            }
            finally
            {
                DestroyIcon(fileInfo.hIcon);
            }
        }
        catch
        {
            return null;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
