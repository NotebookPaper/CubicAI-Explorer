using System.Collections.Specialized;
using System.IO;
using System.Windows;

namespace CubicAIExplorer.Services;

public sealed class ClipboardService : IClipboardService
{
    private const string PreferredDropEffect = "Preferred DropEffect";
    private const uint DropEffectCopy = 5;
    private const uint DropEffectMove = 2;

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
        dataObject.SetData(PreferredDropEffect, CreateDropEffectStream(isCut ? DropEffectMove : DropEffectCopy));

        Clipboard.SetDataObject(dataObject, copy: true);
    }

    public (IReadOnlyList<string> Paths, bool IsCut) GetFiles()
    {
        if (!Clipboard.ContainsFileDropList())
            return ([], false);

        var files = Clipboard.GetFileDropList().Cast<string>().ToArray();
        return (files, ReadDropEffect() == DropEffectMove);
    }

    public bool HasFiles() => Clipboard.ContainsFileDropList();

    public void Clear() => Clipboard.Clear();

    private static MemoryStream CreateDropEffectStream(uint dropEffect)
    {
        var stream = new MemoryStream(4);
        using var writer = new BinaryWriter(stream, System.Text.Encoding.Default, leaveOpen: true);
        writer.Write(dropEffect);
        stream.Position = 0;
        return stream;
    }

    private static uint ReadDropEffect()
    {
        var dataObject = Clipboard.GetDataObject();
        if (dataObject?.GetDataPresent(PreferredDropEffect) != true)
            return DropEffectCopy;

        if (dataObject.GetData(PreferredDropEffect) is not MemoryStream stream)
            return DropEffectCopy;

        if (stream.Length < 4)
            return DropEffectCopy;

        stream.Position = 0;
        using var reader = new BinaryReader(stream, System.Text.Encoding.Default, leaveOpen: true);
        return reader.ReadUInt32();
    }
}
