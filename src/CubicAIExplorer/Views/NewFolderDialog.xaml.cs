using System.Windows;

namespace CubicAIExplorer.Views;

public partial class NewFolderDialog : Window
{
    public string FolderName => FolderNameTextBox.Text.Trim();

    public NewFolderDialog()
    {
        InitializeComponent();
        FolderNameTextBox.Text = "New folder";
        Loaded += (_, _) =>
        {
            FolderNameTextBox.Focus();
            FolderNameTextBox.SelectAll();
        };
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(FolderName))
            return;

        DialogResult = true;
    }
}
