using System.Windows;
using CubicAIExplorer.Models;

namespace CubicAIExplorer.Services;

public interface IDialogService
{
    bool CanShowDialogs { get; }
    void ShowMessage(string message, string title, MessageBoxImage image = MessageBoxImage.Information);
    bool ShowConfirmation(string message, string title, MessageBoxImage image = MessageBoxImage.Question);
    FileTransferCollisionResolution? ShowFileConflictDialog(int conflictCount);
    IReadOnlyList<BatchRenamePreviewItem>? ShowBatchRenameDialog(
        IReadOnlyList<FileSystemItem> items,
        IReadOnlyList<string> siblingNames,
        BatchRenameService batchRenameService);
}
