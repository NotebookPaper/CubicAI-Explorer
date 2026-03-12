using System.Globalization;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace CubicAIExplorer.Views;

public partial class SplitFileDialog : Window
{
    private sealed record ChunkSizeOption(string Label, long Bytes, bool IsCustom = false)
    {
        public override string ToString() => Label;
    }

    public SplitFileDialog(string? sourcePath = null)
    {
        InitializeComponent();

        ChunkSizeComboBox.ItemsSource = new[]
        {
            new ChunkSizeOption("10 MB", 10L * 1024 * 1024),
            new ChunkSizeOption("100 MB", 100L * 1024 * 1024),
            new ChunkSizeOption("700 MB (CD)", 700L * 1024 * 1024),
            new ChunkSizeOption("4.7 GB (DVD)", (long)(4.7 * 1024 * 1024 * 1024)),
            new ChunkSizeOption("Custom", 0, IsCustom: true)
        };
        ChunkSizeComboBox.SelectedIndex = 1;

        if (!string.IsNullOrWhiteSpace(sourcePath))
        {
            SourcePathTextBox.Text = sourcePath;
            OutputDirectoryTextBox.Text = Path.GetDirectoryName(sourcePath) ?? string.Empty;
        }
    }

    public string SourcePath => SourcePathTextBox.Text.Trim();

    public string OutputDirectory => OutputDirectoryTextBox.Text.Trim();

    public long ChunkSizeBytes
    {
        get
        {
            if (ChunkSizeComboBox.SelectedItem is not ChunkSizeOption option)
                return 0;

            if (!option.IsCustom)
                return option.Bytes;

            return TryParseCustomChunkSize(CustomChunkSizeTextBox.Text, out var bytes) ? bytes : 0;
        }
    }

    private void BrowseSource_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            Multiselect = false,
            FileName = SourcePath
        };

        if (dialog.ShowDialog(this) == true)
        {
            SourcePathTextBox.Text = dialog.FileName;
            if (string.IsNullOrWhiteSpace(OutputDirectoryTextBox.Text))
                OutputDirectoryTextBox.Text = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
        }
    }

    private void ChunkSizeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var isCustom = ChunkSizeComboBox.SelectedItem is ChunkSizeOption { IsCustom: true };
        CustomChunkSizeTextBox.IsEnabled = isCustom;
        if (!isCustom)
            CustomChunkSizeTextBox.Text = string.Empty;
    }

    private void Split_Click(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(SourcePath))
        {
            MessageBox.Show(this, "Choose an existing source file.", "Split File", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(OutputDirectory))
        {
            MessageBox.Show(this, "Enter an output directory.", "Split File", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!Directory.Exists(OutputDirectory))
        {
            MessageBox.Show(this, "Choose an existing output directory.", "Split File", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (ChunkSizeBytes <= 0)
        {
            MessageBox.Show(this, "Enter a valid chunk size.", "Split File", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }

    private static bool TryParseCustomChunkSize(string? rawValue, out long bytes)
    {
        bytes = 0;
        if (!double.TryParse(rawValue, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out var mbValue)
            && !double.TryParse(rawValue, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out mbValue))
        {
            return false;
        }

        if (mbValue <= 0)
            return false;

        bytes = (long)Math.Round(mbValue * 1024 * 1024, MidpointRounding.AwayFromZero);
        return bytes > 0;
    }
}
