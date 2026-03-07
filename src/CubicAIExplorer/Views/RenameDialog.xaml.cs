using System.Windows;

namespace CubicAIExplorer.Views;

public partial class RenameDialog : Window
{
    public string EnteredName => NameTextBox.Text.Trim();

    public RenameDialog(string currentName)
    {
        InitializeComponent();
        NameTextBox.Text = currentName;
        Loaded += (_, _) =>
        {
            NameTextBox.Focus();
            NameTextBox.SelectAll();
        };
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EnteredName))
            return;

        DialogResult = true;
    }
}
