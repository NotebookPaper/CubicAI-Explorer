using System.IO;

namespace CubicAIExplorer.Services;

public static class PathSecurityHelper
{
    public static string? SanitizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            var fullPath = Path.GetFullPath(path);
            if (fullPath.StartsWith(@"\\?\", StringComparison.Ordinal))
                fullPath = fullPath[4..];

            return Path.IsPathRooted(fullPath) ? fullPath : null;
        }
        catch
        {
            return null;
        }
    }

    public static string SanitizePathOrFallback(string? path, string fallbackPath)
        => SanitizePath(path) ?? fallbackPath;
}
