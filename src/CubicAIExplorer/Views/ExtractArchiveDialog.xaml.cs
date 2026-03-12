using System.Windows;
using System.IO;

namespace CubicAIExplorer.Views;

public partial class ExtractArchiveDialog : Window
{
    public string DestinationPath => DestinationTextBox.Text.Trim();
    public bool OpenFolderWhenDone => OpenFolderCheckBox.IsChecked == true;

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
        {
            MessageBox.Show(this, "Enter a destination folder path.", "Extract Archive", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var fullPath = Path.GetFullPath(DestinationPath);
            var parentDirectory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(parentDirectory) || !Directory.Exists(parentDirectory))
            {
                MessageBox.Show(this, "Choose a destination whose parent folder already exists.", "Extract Archive", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        catch
        {
            MessageBox.Show(this, "Enter a valid destination folder path.", "Extract Archive", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }
}
