using System.Collections.ObjectModel;
using System.Windows;
using CubicAIExplorer.Models;
using CubicAIExplorer.Services;

namespace CubicAIExplorer.Views;

public partial class BatchRenameDialog : Window
{
    private readonly IReadOnlyList<FileSystemItem> _items;
    private readonly IReadOnlyList<string> _siblingNames;
    private readonly BatchRenameService _batchRenameService;

    public ObservableCollection<BatchRenamePreviewItem> PreviewItems { get; } = [];
    public string HeaderText => $"Previewing {_items.Count} selected item(s)";
    public IReadOnlyList<BatchRenamePreviewItem> RenamePlan { get; private set; } = [];

    public BatchRenameDialog(
        IReadOnlyList<FileSystemItem> items,
        IReadOnlyList<string> siblingNames,
        BatchRenameService? batchRenameService = null)
    {
        _items = items;
        _siblingNames = siblingNames;
        _batchRenameService = batchRenameService ?? new BatchRenameService();

        InitializeComponent();
        DataContext = this;

        CaseModeComboBox.ItemsSource = new[]
        {
            BatchRenameCaseMode.None,
            BatchRenameCaseMode.Lowercase,
            BatchRenameCaseMode.Uppercase,
            BatchRenameCaseMode.TitleCase,
            BatchRenameCaseMode.SentenceCase
        };
        CounterPositionComboBox.ItemsSource = new[]
        {
            BatchRenameCounterPosition.None,
            BatchRenameCounterPosition.Prefix,
            BatchRenameCounterPosition.Suffix
        };
        ExtensionModeComboBox.ItemsSource = new[]
        {
            BatchRenameExtensionMode.Keep,
            BatchRenameExtensionMode.Remove,
            BatchRenameExtensionMode.Replace
        };

        CaseModeComboBox.SelectedItem = BatchRenameCaseMode.None;
        CounterPositionComboBox.SelectedItem = BatchRenameCounterPosition.None;
        CounterStartTextBox.Text = "1";
        CounterPaddingTextBox.Text = "2";
        CounterSeparatorTextBox.Text = "_";
        ExtensionModeComboBox.SelectedItem = BatchRenameExtensionMode.Keep;

        Loaded += (_, _) => RefreshPreview();
    }

    public IReadOnlyList<BatchRenamePreviewItem> BuildPreview(BatchRenameOptions options)
        => _batchRenameService.BuildPreview(_items, _siblingNames, options);

    private void OptionsChanged(object sender, RoutedEventArgs e)
    {
        RefreshPreview();
    }

    private void OnRenameClick(object sender, RoutedEventArgs e)
    {
        RefreshPreview();
        if (RenamePlan.Count == 0)
            return;

        DialogResult = true;
    }

    private void RefreshPreview()
    {
        var options = ReadOptions();
        RenamePlan = BuildPreview(options);

        PreviewItems.Clear();
        foreach (var item in RenamePlan)
            PreviewItems.Add(item);

        ExtensionTextBox.IsEnabled = options.ExtensionMode == BatchRenameExtensionMode.Replace;
    }

    private BatchRenameOptions ReadOptions()
    {
        return new BatchRenameOptions(
            FindTextBox.Text,
            ReplaceTextBox.Text,
            CaseModeComboBox.SelectedItem is BatchRenameCaseMode caseMode ? caseMode : BatchRenameCaseMode.None,
            CounterPositionComboBox.SelectedItem is BatchRenameCounterPosition counterPosition ? counterPosition : BatchRenameCounterPosition.None,
            ParseOrDefault(CounterStartTextBox.Text, 1),
            ParseOrDefault(CounterPaddingTextBox.Text, 2),
            CounterSeparatorTextBox.Text ?? string.Empty,
            ExtensionModeComboBox.SelectedItem is BatchRenameExtensionMode extensionMode ? extensionMode : BatchRenameExtensionMode.Keep,
            ExtensionTextBox.Text ?? string.Empty);
    }

    private static int ParseOrDefault(string? value, int fallback)
        => int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;
}
