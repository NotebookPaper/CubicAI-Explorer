using System.Globalization;
using System.IO;
using CubicAIExplorer.Models;

namespace CubicAIExplorer.Services;

public sealed class BatchRenameService
{
    public IReadOnlyList<BatchRenamePreviewItem> BuildPreview(
        IReadOnlyList<FileSystemItem> items,
        IReadOnlyCollection<string>? siblingNames,
        BatchRenameOptions options)
    {
        if (items.Count == 0)
            return [];

        var reservedNames = new HashSet<string>(
            siblingNames ?? items.Select(static item => item.Name).ToArray(),
            StringComparer.OrdinalIgnoreCase);
        foreach (var selectedName in items.Select(static item => item.Name))
            reservedNames.Remove(selectedName);

        var usedNewNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<BatchRenamePreviewItem>(items.Count);

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var proposedName = TransformName(item, index, options);
            var uniqueName = EnsureUniqueName(proposedName, reservedNames, usedNewNames, item.ItemType != FileSystemItemType.File);
            usedNewNames.Add(uniqueName);
            results.Add(new BatchRenamePreviewItem(item.FullPath, item.Name, uniqueName));
        }

        return results;
    }

    public void ApplyRenamePlan(IFileSystemService fileSystemService, IReadOnlyList<BatchRenamePreviewItem> plan)
    {
        if (plan.Count == 0)
            return;

        var directory = Path.GetDirectoryName(plan[0].OriginalPath)
            ?? throw new InvalidOperationException("Batch rename requires a parent directory.");
        var activePaths = plan.ToDictionary(item => item.OriginalPath, item => item.OriginalPath, StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var item in plan)
            {
                var tempName = $".cubicai-batchrename-{Guid.NewGuid():N}{Path.GetExtension(item.OriginalName)}";
                var stagedPath = fileSystemService.RenameFile(item.OriginalPath, tempName);
                activePaths[item.OriginalPath] = stagedPath;
            }

            foreach (var item in plan)
            {
                var stagedPath = activePaths[item.OriginalPath];
                var finalPath = Path.Combine(directory, item.NewName);

                if (string.Equals(stagedPath, finalPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                var renamedPath = fileSystemService.RenameFile(stagedPath, item.NewName);
                if (!string.Equals(Path.GetFileName(renamedPath), item.NewName, StringComparison.OrdinalIgnoreCase))
                    throw new IOException($"Could not rename '{item.OriginalName}' to '{item.NewName}' because the target name is unavailable.");

                activePaths[item.OriginalPath] = renamedPath;
            }
        }
        catch
        {
            RestoreOriginalNames(fileSystemService, plan, activePaths);
            throw;
        }
    }

    private static void RestoreOriginalNames(
        IFileSystemService fileSystemService,
        IReadOnlyList<BatchRenamePreviewItem> plan,
        IReadOnlyDictionary<string, string> activePaths)
    {
        foreach (var item in plan.Reverse())
        {
            if (!activePaths.TryGetValue(item.OriginalPath, out var currentPath))
                continue;

            if (string.Equals(currentPath, item.OriginalPath, StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                fileSystemService.RenameFile(currentPath, item.OriginalName);
            }
            catch
            {
                // Best-effort rollback only.
            }
        }
    }

    private static string TransformName(FileSystemItem item, int index, BatchRenameOptions options)
    {
        var isFile = item.ItemType == FileSystemItemType.File;
        var baseName = isFile ? Path.GetFileNameWithoutExtension(item.Name) : item.Name;
        var extension = isFile ? Path.GetExtension(item.Name) : string.Empty;

        if (!string.IsNullOrEmpty(options.FindText))
            baseName = baseName.Replace(options.FindText, options.ReplaceText, StringComparison.CurrentCulture);

        baseName = ApplyCase(baseName, options.CaseMode);

        if (options.CounterPosition != BatchRenameCounterPosition.None)
        {
            var counterText = $"{options.CounterSeparator}{options.FormatCounter(index)}";
            baseName = options.CounterPosition == BatchRenameCounterPosition.Prefix
                ? $"{options.FormatCounter(index)}{options.CounterSeparator}{baseName}"
                : $"{baseName}{counterText}";
        }

        if (string.IsNullOrWhiteSpace(baseName))
            baseName = isFile ? "Renamed File" : "Renamed Folder";

        if (!isFile)
            return baseName;

        var resolvedExtension = options.ExtensionMode switch
        {
            BatchRenameExtensionMode.Remove => string.Empty,
            BatchRenameExtensionMode.Replace => NormalizeExtension(options.NewExtension),
            _ => extension
        };

        return $"{baseName}{resolvedExtension}";
    }

    private static string ApplyCase(string value, BatchRenameCaseMode mode)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return mode switch
        {
            BatchRenameCaseMode.Lowercase => value.ToLower(CultureInfo.CurrentCulture),
            BatchRenameCaseMode.Uppercase => value.ToUpper(CultureInfo.CurrentCulture),
            BatchRenameCaseMode.TitleCase => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLower(CultureInfo.CurrentCulture)),
            BatchRenameCaseMode.SentenceCase => char.ToUpper(value[0], CultureInfo.CurrentCulture) + value[1..].ToLower(CultureInfo.CurrentCulture),
            _ => value
        };
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return string.Empty;

        var trimmed = extension.Trim();
        return trimmed.StartsWith('.') ? trimmed : $".{trimmed}";
    }

    private static string EnsureUniqueName(
        string proposedName,
        IReadOnlySet<string> reservedNames,
        IReadOnlySet<string> usedNewNames,
        bool isDirectory)
    {
        if (!reservedNames.Contains(proposedName) && !usedNewNames.Contains(proposedName))
            return proposedName;

        var stem = isDirectory ? proposedName : Path.GetFileNameWithoutExtension(proposedName);
        var extension = isDirectory ? string.Empty : Path.GetExtension(proposedName);
        var suffix = 2;

        while (true)
        {
            var candidate = $"{stem} ({suffix}){extension}";
            if (!reservedNames.Contains(candidate) && !usedNewNames.Contains(candidate))
                return candidate;
            suffix++;
        }
    }
}
