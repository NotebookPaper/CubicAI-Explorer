using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using CubicAIExplorer.Models;

namespace CubicAIExplorer.Views;

public partial class ChecksumDialog : Window
{
    private readonly Func<string, Task<FileChecksumSet>> _computeChecksumsAsync;
    private FileChecksumSet? _currentChecksums;

    public ChecksumDialog(string? initialPath, Func<string, Task<FileChecksumSet>> computeChecksumsAsync)
    {
        _computeChecksumsAsync = computeChecksumsAsync;

        InitializeComponent();

        ComparisonAlgorithmComboBox.ItemsSource = new[]
        {
            "MD5",
            "SHA1",
            "SHA256"
        };
        ComparisonAlgorithmComboBox.SelectedIndex = 0;

        if (!string.IsNullOrWhiteSpace(initialPath))
            FilePathTextBox.Text = initialPath;

        Loaded += async (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(FilePathTextBox.Text) && File.Exists(FilePathTextBox.Text))
                await GenerateChecksumsAsync();
        };
    }

    public string FilePath => FilePathTextBox.Text.Trim();

    public void LoadChecksums(FileChecksumSet checksums)
    {
        _currentChecksums = checksums;
        Md5TextBox.Text = checksums.Md5;
        Sha1TextBox.Text = checksums.Sha1;
        Sha256TextBox.Text = checksums.Sha256;
        UpdateComparisonResult();
    }

    public static bool IsChecksumMatch(string actualChecksum, string? comparisonValue)
    {
        if (string.IsNullOrWhiteSpace(actualChecksum) || string.IsNullOrWhiteSpace(comparisonValue))
            return false;

        static string Normalize(string value)
            => value.Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Trim()
                .ToLowerInvariant();

        return string.Equals(Normalize(actualChecksum), Normalize(comparisonValue), StringComparison.Ordinal);
    }

    private void BrowseFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            Multiselect = false,
            FileName = FilePath
        };

        if (dialog.ShowDialog(this) == true)
            FilePathTextBox.Text = dialog.FileName;
    }

    private async void Generate_Click(object sender, RoutedEventArgs e)
    {
        await GenerateChecksumsAsync();
    }

    private async Task GenerateChecksumsAsync()
    {
        if (!File.Exists(FilePath))
        {
            MessageBox.Show(this, "Choose an existing file.", "Checksum Tool", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ComparisonResultTextBlock.Text = "Generating checksums...";

        try
        {
            LoadChecksums(await _computeChecksumsAsync(FilePath));
        }
        catch (OperationCanceledException)
        {
            ComparisonResultTextBlock.Text = "Checksum generation canceled.";
        }
        catch (Exception ex)
        {
            ComparisonResultTextBlock.Text = $"Checksum generation failed: {ex.Message}";
        }
    }

    private void ComparisonInputChanged(object sender, EventArgs e)
    {
        UpdateComparisonResult();
    }

    private void UpdateComparisonResult()
    {
        if (_currentChecksums == null)
        {
            ComparisonResultTextBlock.Text = "Generate checksums to compare.";
            return;
        }

        var selectedAlgorithm = ComparisonAlgorithmComboBox.SelectedItem as string ?? "MD5";
        var actual = selectedAlgorithm switch
        {
            "SHA1" => _currentChecksums.Sha1,
            "SHA256" => _currentChecksums.Sha256,
            _ => _currentChecksums.Md5
        };

        if (string.IsNullOrWhiteSpace(ComparisonTextBox.Text))
        {
            ComparisonResultTextBlock.Text = $"{selectedAlgorithm} ready for comparison.";
            return;
        }

        ComparisonResultTextBlock.Text = IsChecksumMatch(actual, ComparisonTextBox.Text)
            ? $"{selectedAlgorithm} matches."
            : $"{selectedAlgorithm} does not match.";
    }
}
