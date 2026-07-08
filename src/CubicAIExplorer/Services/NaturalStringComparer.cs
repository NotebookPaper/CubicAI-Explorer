using System.Runtime.InteropServices;

namespace CubicAIExplorer.Services;

/// <summary>
/// Explorer-style logical string comparison ("file2" before "file10"),
/// matching the shell sort order the original CubicExplorer inherited from
/// the Windows shell namespace.
/// </summary>
public sealed class NaturalStringComparer : IComparer<string>
{
    public static NaturalStringComparer Instance { get; } = new();

    private NaturalStringComparer()
    {
    }

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x == null) return -1;
        if (y == null) return 1;
        return StrCmpLogicalW(x, y);
    }

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int StrCmpLogicalW(string psz1, string psz2);
}
