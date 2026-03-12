using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace CubicAIExplorer.Services;

public sealed class ClipboardService : IClipboardService
{
    private const string PreferredDropEffect = "Preferred DropEffect";
    private const uint DropEffectCopy = 5;
    private const uint DropEffectMove = 2;
    private const int ClipboardRetryCount = 5;
    private const int ClipboardRetryDelayMs = 25;

    public void SetFiles(IEnumerable<string> paths, bool isCut)
    {
        var filePaths = paths
            .Where(static p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (filePaths.Length == 0)
            return;

        var dropList = new StringCollection();
        dropList.AddRange(filePaths);

        var dataObject = new DataObject();
        dataObject.SetFileDropList(dropList);
        dataObject.SetData(PreferredDropEffect, CreateDropEffectBytes(isCut ? DropEffectMove : DropEffectCopy));

        ExecuteClipboardAction(() => Clipboard.SetDataObject(dataObject, copy: true));
    }

    public (IReadOnlyList<string> Paths, bool IsCut) GetFiles()
    {
        if (!ExecuteClipboardFunc(Clipboard.ContainsFileDropList))
            return ([], false);

        var files = ExecuteClipboardFunc(() => Clipboard.GetFileDropList().Cast<string>().ToArray());
        return (files, ReadDropEffect() == DropEffectMove);
    }

    public bool HasFiles() => ExecuteClipboardFunc(Clipboard.ContainsFileDropList);

    public void Clear() => ExecuteClipboardAction(Clipboard.Clear);

    private static byte[] CreateDropEffectBytes(uint dropEffect) => BitConverter.GetBytes(dropEffect);

    private static uint ReadDropEffect()
    {
        var dataObject = ExecuteClipboardFunc(Clipboard.GetDataObject);
        if (dataObject?.GetDataPresent(PreferredDropEffect) != true)
            return DropEffectCopy;

        return ReadDropEffectValue(dataObject.GetData(PreferredDropEffect));
    }

    public static uint ReadDropEffectValue(object? value)
    {
        if (value is MemoryStream memoryStream)
            return ReadDropEffectFromStream(memoryStream);

        if (value is byte[] bytes)
            return bytes.Length >= 4 ? BitConverter.ToUInt32(bytes, 0) : DropEffectCopy;

        if (value is Stream stream)
            return ReadDropEffectFromStream(stream);

        return DropEffectCopy;
    }

    private static uint ReadDropEffectFromStream(Stream stream)
    {
        if (!stream.CanSeek || stream.Length < 4)
            return DropEffectCopy;

        stream.Position = 0;
        using var reader = new BinaryReader(stream, System.Text.Encoding.Default, leaveOpen: true);
        return reader.ReadUInt32();
    }

    private static void ExecuteClipboardAction(Action action)
    {
        ExecuteClipboardFunc(() =>
        {
            action();
            return true;
        });
    }

    private static T ExecuteClipboardFunc<T>(Func<T> action)
    {
        for (var attempt = 0; attempt < ClipboardRetryCount; attempt++)
        {
            try
            {
                return action();
            }
            catch (ExternalException) when (attempt < ClipboardRetryCount - 1)
            {
                Thread.Sleep(ClipboardRetryDelayMs);
            }
        }

        return action();
    }
}
