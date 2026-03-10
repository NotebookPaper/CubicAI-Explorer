using System.Windows;

namespace CubicAIExplorer.Views;

public partial class NewFolderDialog : Window
{
    public string FolderName
    {
        get => FolderNameTextBox.Text.Trim();
        set => FolderNameTextBox.Text = value;
    }

    public string Message
    {
        get => MessageLabel.Text;
        set => MessageLabel.Text = value;
    }

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
