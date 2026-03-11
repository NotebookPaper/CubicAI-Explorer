using System.Globalization;

namespace CubicAIExplorer.Models;

public enum BatchRenameCaseMode
{
    None,
    Lowercase,
    Uppercase,
    TitleCase,
    SentenceCase
}

public enum BatchRenameCounterPosition
{
    None,
    Prefix,
    Suffix
}

public enum BatchRenameExtensionMode
{
    Keep,
    Remove,
    Replace
}

public sealed record BatchRenameOptions(
    string FindText,
    string ReplaceText,
    BatchRenameCaseMode CaseMode,
    BatchRenameCounterPosition CounterPosition,
    int CounterStart,
    int CounterPadding,
    string CounterSeparator,
    BatchRenameExtensionMode ExtensionMode,
    string NewExtension)
{
    public static BatchRenameOptions Default { get; } = new(
        string.Empty,
        string.Empty,
        BatchRenameCaseMode.None,
        BatchRenameCounterPosition.None,
        1,
        2,
        "_",
        BatchRenameExtensionMode.Keep,
        string.Empty);

    public string FormatCounter(int index)
    {
        var numericValue = Math.Max(0, CounterStart) + index;
        var digits = Math.Max(1, CounterPadding);
        return numericValue.ToString(new string('0', digits), CultureInfo.InvariantCulture);
    }
}

public sealed record BatchRenamePreviewItem(
    string OriginalPath,
    string OriginalName,
    string NewName);
