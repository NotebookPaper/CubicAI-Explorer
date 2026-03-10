using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CubicAIExplorer.Services;
using CubicAIExplorer.ViewModels;

namespace CubicAIExplorer.Views;

public partial class ArchiveBrowserDialog : Window
{
    private readonly string _archivePath;
    private readonly IFileSystemService _fileSystemService;
    private readonly FileListViewModel _sourceFileList;
    private List<ArchiveEntryInfo> _allEntries;
    private readonly ObservableCollection<ArchiveEntryViewModel> _visibleEntries = [];
    private string _currentFolder = string.Empty;

    public ArchiveBrowserDialog(string archivePath, IReadOnlyList<ArchiveEntryInfo> entries, FileListViewModel sourceFileList, IFileSystemService fileSystemService)
    {
        InitializeComponent();

        _archivePath = archivePath;
        _fileSystemService = fileSystemService;
        _sourceFileList = sourceFileList;
        _allEntries = entries.ToList();
        ArchivePathTextBlock.Text = archivePath;
        EntriesListView.ItemsSource = _visibleEntries;

        RefreshEntries();
        Loaded += (_, _) => FilterTextBox.Focus();
    }

    private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshEntries();

    private void FoldersOnlyCheckBox_Changed(object sender, RoutedEventArgs e) => RefreshEntries();

    private void UpButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFolder))
            return;

        var trimmed = _currentFolder.TrimEnd('/');
        var parentSlashIndex = trimmed.LastIndexOf('/');
        _currentFolder = parentSlashIndex >= 0 ? trimmed[..(parentSlashIndex + 1)] : string.Empty;
        RefreshEntries();
    }

    private void EntriesListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (EntriesListView.SelectedItem is ArchiveEntryViewModel { IsDirectory: true } entry)
        {
            _currentFolder = entry.FullName;
            RefreshEntries();
        }
    }

    private async void ExtractSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedEntries = EntriesListView.SelectedItems.OfType<ArchiveEntryViewModel>().ToArray();
        if (selectedEntries.Length == 0)
            return;

        await ExtractEntriesAsync(selectedEntries.Select(static entry => entry.FullName), promptForDestination: true);
    }

    private async void ExtractHereButton_Click(object sender, RoutedEventArgs e)
    {
        await ExtractEntriesAsync(GetCurrentScopeEntryPaths(), promptForDestination: false);
    }

    private async void ExtractToButton_Click(object sender, RoutedEventArgs e)
    {
        await ExtractEntriesAsync(GetCurrentScopeEntryPaths(), promptForDestination: true);
    }

    private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedEntries = EntriesListView.SelectedItems.OfType<ArchiveEntryViewModel>().ToArray();
        if (selectedEntries.Length == 0)
            return;

        var names = selectedEntries.Length <= 3
            ? string.Join(", ", selectedEntries.Select(static entry => entry.Name))
            : $"{selectedEntries.Length} items";
        var result = MessageBox.Show(
            $"Permanently delete {names} from the archive?",
            "Delete from Archive",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            IsEnabled = false;
            _fileSystemService.DeleteArchiveEntries(_archivePath, selectedEntries.Select(static e => e.FullName));
            _allEntries = _fileSystemService.GetArchiveEntries(_archivePath, int.MaxValue).ToList();
            RefreshEntries();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Delete failed: {ex.Message}",
                "Archive Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private async Task ExtractEntriesAsync(IEnumerable<string> entryPaths, bool promptForDestination)
    {
        var requestedEntries = entryPaths
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (requestedEntries.Length == 0)
            return;

        var defaultDestination = GetDefaultDestination();
        var destination = defaultDestination;
        var openWhenDone = false;
        var conflictMode = Services.ArchiveExtractConflictMode.Skip;

        if (promptForDestination)
        {
            var dialog = new ExtractArchiveDialog(Path.GetFileName(_archivePath), defaultDestination) { Owner = this };
            if (dialog.ShowDialog() != true)
                return;

            destination = dialog.DestinationPath;
            openWhenDone = dialog.OpenFolderWhenDone;
            conflictMode = dialog.ConflictMode;
        }

        try
        {
            IsEnabled = false;
            await _sourceFileList.ExtractArchiveEntriesToAsync(_archivePath, requestedEntries, destination, openWhenDone, conflictMode);
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private string GetDefaultDestination()
    {
        var archiveDirectory = Path.GetDirectoryName(_archivePath) ?? _archivePath;
        var archiveName = Path.GetFileNameWithoutExtension(_archivePath);
        return Path.Combine(archiveDirectory, archiveName);
    }

    private IEnumerable<string> GetCurrentScopeEntryPaths()
    {
        if (string.IsNullOrEmpty(_currentFolder))
            return _allEntries.Select(static entry => entry.FullName);

        return _allEntries
            .Where(entry => entry.FullName.StartsWith(_currentFolder, StringComparison.OrdinalIgnoreCase))
            .Select(static entry => entry.FullName);
    }

    private void RefreshEntries()
    {
        var filter = FilterTextBox.Text.Trim();
        var foldersOnly = FoldersOnlyCheckBox.IsChecked == true;
        var currentItems = BuildCurrentFolderItems()
            .Where(entry =>
                (!foldersOnly || entry.IsDirectory)
                && (string.IsNullOrWhiteSpace(filter)
                    || entry.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    || entry.FullName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    || entry.EntryType.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(static entry => entry.IsDirectory)
            .ThenBy(static entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _visibleEntries.Clear();
        foreach (var entry in currentItems)
            _visibleEntries.Add(entry);

        UpButton.IsEnabled = !string.IsNullOrEmpty(_currentFolder);
        CurrentFolderTextBlock.Text = string.IsNullOrEmpty(_currentFolder) ? @"Inside: \" : $@"Inside: \{_currentFolder}";
        SummaryTextBlock.Text = BuildSummary(_allEntries.Count, currentItems, _currentFolder);
    }

    private List<ArchiveEntryViewModel> BuildCurrentFolderItems()
    {
        var folderMap = new Dictionary<string, ArchiveEntryViewModel>(StringComparer.OrdinalIgnoreCase);
        var prefix = _currentFolder;

        foreach (var entry in _allEntries)
        {
            if (!entry.FullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var remainingPath = entry.FullName[prefix.Length..];
            if (string.IsNullOrEmpty(remainingPath))
                continue;

            var slashIndex = remainingPath.IndexOf('/');
            if (slashIndex >= 0)
            {
                var folderName = remainingPath[..slashIndex];
                if (string.IsNullOrWhiteSpace(folderName))
                    continue;

                var folderFullName = prefix + folderName + "/";
                if (!folderMap.ContainsKey(folderFullName))
                    folderMap[folderFullName] = ArchiveEntryViewModel.CreateFolder(folderName, prefix, folderFullName);
                continue;
            }

            if (!folderMap.ContainsKey(entry.FullName))
            {
                folderMap[entry.FullName] = ArchiveEntryViewModel.CreateEntry(entry, prefix);
            }
        }

        return folderMap.Values.ToList();
    }

    private static string BuildSummary(int totalCount, IReadOnlyCollection<ArchiveEntryViewModel> visibleEntries, string currentFolder)
    {
        var fileCount = visibleEntries.Count(static entry => !entry.IsDirectory);
        var folderCount = visibleEntries.Count(static entry => entry.IsDirectory);
        var scopeLabel = string.IsNullOrEmpty(currentFolder) ? "root" : currentFolder.TrimEnd('/');

        return $"{visibleEntries.Count} item(s) in {scopeLabel} "
             + $"({folderCount} folder{(folderCount == 1 ? string.Empty : "s")}, {fileCount} file{(fileCount == 1 ? string.Empty : "s")})"
             + $" | {totalCount} archive entr{(totalCount == 1 ? "y" : "ies")} total";
    }

    private sealed class ArchiveEntryViewModel
    {
        private ArchiveEntryViewModel(string name, string folderPath, string fullName, string entryType, string displaySize, bool isDirectory)
        {
            Name = name;
            FolderPath = folderPath;
            FullName = fullName;
            EntryType = entryType;
            DisplaySize = displaySize;
            IsDirectory = isDirectory;
        }

        public string Name { get; }
        public string FolderPath { get; }
        public string FullName { get; }
        public string EntryType { get; }
        public string DisplaySize { get; }
        public bool IsDirectory { get; }

        public static ArchiveEntryViewModel CreateEntry(ArchiveEntryInfo entry, string currentFolder)
        {
            var relativePath = entry.FullName[currentFolder.Length..];
            return new ArchiveEntryViewModel(
                entry.IsDirectory ? relativePath.TrimEnd('/') : relativePath,
                string.IsNullOrEmpty(currentFolder) ? @"\" : $@"\{currentFolder.TrimEnd('/')}",
                entry.FullName,
                entry.IsDirectory ? "Folder" : "File",
                entry.IsDirectory ? string.Empty : FormatSize(entry.Length),
                entry.IsDirectory);
        }

        public static ArchiveEntryViewModel CreateFolder(string name, string currentFolder, string fullName)
            => new(name,
                string.IsNullOrEmpty(currentFolder) ? @"\" : $@"\{currentFolder.TrimEnd('/')}",
                fullName,
                "Folder",
                string.Empty,
                isDirectory: true);

        private static string FormatSize(long bytes) => bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
            _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
        };
    }
}
