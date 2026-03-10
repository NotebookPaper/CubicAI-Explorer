using System.Windows;

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
            return;

        DialogResult = true;
    }
}
