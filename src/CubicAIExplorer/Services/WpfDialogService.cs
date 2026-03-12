using System.Windows;
using CubicAIExplorer.Models;
using CubicAIExplorer.Views;

namespace CubicAIExplorer.Services;

public sealed class WpfDialogService : IDialogService
{
    public bool CanShowDialogs => true;

    public void ShowMessage(string message, string title, MessageBoxImage image = MessageBoxImage.Information)
    {
        MessageBox.Show(GetOwner(), message, title, MessageBoxButton.OK, image);
    }

    public bool ShowConfirmation(string message, string title, MessageBoxImage image = MessageBoxImage.Question)
    {
        var result = MessageBox.Show(GetOwner(), message, title, MessageBoxButton.YesNo, image);
        return result == MessageBoxResult.Yes;
    }

    public FileTransferCollisionResolution? ShowFileConflictDialog(int conflictCount)
    {
        var dialog = new FileConflictDialog(conflictCount)
        {
            Owner = GetOwner()
        };

        return dialog.ShowDialog() == true ? dialog.Resolution : null;
    }

    public IReadOnlyList<BatchRenamePreviewItem>? ShowBatchRenameDialog(
        IReadOnlyList<FileSystemItem> items,
        IReadOnlyList<string> siblingNames,
        BatchRenameService batchRenameService)
    {
        var dialog = new BatchRenameDialog(items, siblingNames, batchRenameService)
        {
            Owner = GetOwner()
        };

        return dialog.ShowDialog() == true ? dialog.RenamePlan : null;
    }

    private static Window? GetOwner() => Application.Current?.MainWindow;
}
