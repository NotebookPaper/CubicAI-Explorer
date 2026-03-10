using System.Windows;
using CubicAIExplorer.Services;

namespace CubicAIExplorer.Views;

public partial class ExtractArchiveDialog : Window
{
    public string DestinationPath => DestinationTextBox.Text.Trim();
    public bool OpenFolderWhenDone => OpenFolderCheckBox.IsChecked == true;
    public ArchiveExtractConflictMode ConflictMode =>
        ConflictOverwriteRadio.IsChecked == true ? ArchiveExtractConflictMode.Overwrite :
        ConflictRenameRadio.IsChecked == true ? ArchiveExtractConflictMode.RenameWithSuffix :
        ArchiveExtractConflictMode.Skip;

    public ExtractArchiveDialog(string archiveName, string defaultDestination)
    {
        InitializeComponent();
        PromptTextBlock.Text = $"Extract \"{archiveName}\" to:";
        DestinationTextBox.Text = defaultDestination;
        Loaded += (_, _) =>
        {
            DestinationTextBox.Focus();
            DestinationTextBox.SelectAll();
        };
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DestinationPath))
            return;

        DialogResult = true;
    }
}
