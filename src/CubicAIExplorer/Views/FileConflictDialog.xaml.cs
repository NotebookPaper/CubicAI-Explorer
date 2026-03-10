using System.Windows;
using CubicAIExplorer.Services;

namespace CubicAIExplorer.Views;

public partial class FileConflictDialog : Window
{
    public FileTransferCollisionResolution? Resolution { get; private set; }

    public FileConflictDialog(int conflictCount)
    {
        InitializeComponent();
        DetailsTextBlock.Text = conflictCount == 1
            ? "Choose how to handle the existing item."
            : $"Choose how to handle the {conflictCount} existing items.";
    }

    private void OnReplaceClick(object sender, RoutedEventArgs e)
    {
        Resolution = FileTransferCollisionResolution.Replace;
        DialogResult = true;
    }

    private void OnKeepBothClick(object sender, RoutedEventArgs e)
    {
        Resolution = FileTransferCollisionResolution.KeepBoth;
        DialogResult = true;
    }

    private void OnSkipClick(object sender, RoutedEventArgs e)
    {
        Resolution = FileTransferCollisionResolution.Skip;
        DialogResult = true;
    }
}
