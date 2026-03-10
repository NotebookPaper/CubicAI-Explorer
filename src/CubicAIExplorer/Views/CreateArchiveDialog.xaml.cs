using System.IO;
using System.Windows;

namespace CubicAIExplorer.Views;

public partial class CreateArchiveDialog : Window
{
    public string ArchiveName => ArchiveNameTextBox.Text.Trim();
    public string DestinationFolder => DestinationTextBox.Text.Trim();
    public bool OpenFolderWhenDone => OpenFolderCheckBox.IsChecked == true;

    public string ArchivePath => Path.Combine(DestinationFolder,
        ArchiveName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
            ? ArchiveName
            : ArchiveName + ".zip");

    public CreateArchiveDialog(string defaultName, string defaultDestination)
    {
        InitializeComponent();
        ArchiveNameTextBox.Text = defaultName;
        DestinationTextBox.Text = defaultDestination;
        Loaded += (_, _) =>
        {
            ArchiveNameTextBox.Focus();
            ArchiveNameTextBox.SelectAll();
        };
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ArchiveName) || string.IsNullOrWhiteSpace(DestinationFolder))
            return;

        DialogResult = true;
    }
}
