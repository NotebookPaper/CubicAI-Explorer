using System.Windows;
using Microsoft.Win32;

namespace CubicAIExplorer.Views;

public partial class AddToArchiveDialog : Window
{
    public string ArchivePath => ArchivePathTextBox.Text.Trim();

    public AddToArchiveDialog(int itemCount, string defaultArchivePath = "")
    {
        InitializeComponent();
        PromptTextBlock.Text = $"Add {itemCount} item{(itemCount == 1 ? string.Empty : "s")} to archive:";
        ArchivePathTextBox.Text = defaultArchivePath;
        Loaded += (_, _) =>
        {
            ArchivePathTextBox.Focus();
            ArchivePathTextBox.SelectAll();
        };
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select archive",
            Filter = "ZIP archives (*.zip)|*.zip",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == true)
            ArchivePathTextBox.Text = dialog.FileName;
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ArchivePath))
            return;

        DialogResult = true;
    }
}
