using System.IO;
using System.Runtime.InteropServices;
using CubicAIExplorer.Models;

namespace CubicAIExplorer.Services;

public static class ShellFileInfoHelper
{
    private const uint SHGFI_DISPLAYNAME = 0x000000200;
    private const uint SHGFI_TYPENAME = 0x000000400;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

    public static string? TryGetDisplayName(string path)
        => GetShellInfo(path, SHGFI_DISPLAYNAME, inferItemType: null).DisplayName;

    public static string? TryGetTypeName(string path, FileSystemItemType itemType)
        => GetShellInfo(path, SHGFI_TYPENAME, itemType).TypeName;

    private static ShellFileInfo GetShellInfo(string path, uint flags, FileSystemItemType? inferItemType)
    {
        if (string.IsNullOrWhiteSpace(path))
            return ShellFileInfo.Empty;

        var shellInfo = new SHFILEINFO();
        var attributes = FILE_ATTRIBUTE_NORMAL;
        var actualFlags = flags;

        if (!PathExists(path))
        {
            actualFlags |= SHGFI_USEFILEATTRIBUTES;
            attributes = inferItemType is FileSystemItemType.Directory or FileSystemItemType.Drive
                ? FILE_ATTRIBUTE_DIRECTORY
                : LooksLikeDirectoryPath(path)
                    ? FILE_ATTRIBUTE_DIRECTORY
                    : FILE_ATTRIBUTE_NORMAL;
        }

        var result = SHGetFileInfo(
            path,
            attributes,
            ref shellInfo,
            (uint)Marshal.SizeOf<SHFILEINFO>(),
            actualFlags);

        return result == IntPtr.Zero
            ? ShellFileInfo.Empty
            : new ShellFileInfo(NullIfWhiteSpace(shellInfo.szDisplayName), NullIfWhiteSpace(shellInfo.szTypeName));
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    private static bool PathExists(string path) => File.Exists(path) || Directory.Exists(path);

    private static bool LooksLikeDirectoryPath(string path)
    {
        if (path.EndsWith('\\') || path.EndsWith('/'))
            return true;

        return string.IsNullOrWhiteSpace(Path.GetExtension(path));
    }

    private readonly record struct ShellFileInfo(string? DisplayName, string? TypeName)
    {
        public static ShellFileInfo Empty => new(null, null);
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
}
