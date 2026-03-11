using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace CubicAIExplorer.Views;

public partial class JoinFileDialog : Window
{
    public JoinFileDialog(string? firstChunkPath = null)
    {
        InitializeComponent();

        if (!string.IsNullOrWhiteSpace(firstChunkPath))
        {
            FirstChunkPathTextBox.Text = firstChunkPath;
            OutputPathTextBox.Text = GetDefaultOutputPath(firstChunkPath);
        }
    }

    public string FirstChunkPath => FirstChunkPathTextBox.Text.Trim();

    public string OutputPath => OutputPathTextBox.Text.Trim();

    public static string GetDefaultOutputPath(string firstChunkPath)
    {
        var directory = Path.GetDirectoryName(firstChunkPath) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(firstChunkPath);
        return Path.Combine(directory, fileName);
    }

    private void BrowseFirstChunk_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            Multiselect = false,
            FileName = FirstChunkPath
        };

        if (dialog.ShowDialog(this) == true)
        {
            FirstChunkPathTextBox.Text = dialog.FileName;
            if (string.IsNullOrWhiteSpace(OutputPathTextBox.Text))
                OutputPathTextBox.Text = GetDefaultOutputPath(dialog.FileName);
        }
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            FileName = string.IsNullOrWhiteSpace(OutputPath) && !string.IsNullOrWhiteSpace(FirstChunkPath)
                ? GetDefaultOutputPath(FirstChunkPath)
                : OutputPath,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) == true)
            OutputPathTextBox.Text = dialog.FileName;
    }

    private void Join_Click(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(FirstChunkPath))
        {
            MessageBox.Show(this, "Choose an existing first chunk file.", "Join File", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            MessageBox.Show(this, "Enter an output file path.", "Join File", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }
}
