using System.Windows;
using CubicAIExplorer.Models;

namespace CubicAIExplorer.Services;

public sealed class HeadlessDialogService : IDialogService
{
    public static HeadlessDialogService Instance { get; } = new();

    public bool CanShowDialogs => false;

    public void ShowMessage(string message, string title, MessageBoxImage image = MessageBoxImage.Information)
    {
    }

    public bool ShowConfirmation(string message, string title, MessageBoxImage image = MessageBoxImage.Question)
        => false;

    public FileTransferCollisionResolution? ShowFileConflictDialog(int conflictCount)
        => null;

    public IReadOnlyList<BatchRenamePreviewItem>? ShowBatchRenameDialog(
        IReadOnlyList<FileSystemItem> items,
        IReadOnlyList<string> siblingNames,
        BatchRenameService batchRenameService)
        => null;
}
