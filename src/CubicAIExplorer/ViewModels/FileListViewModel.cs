using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CubicAIExplorer.Models;
using CubicAIExplorer.Services;
using CubicAIExplorer.Views;

namespace CubicAIExplorer.ViewModels;

public partial class FileListViewModel : ObservableObject
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IClipboardService _clipboardService;
    private readonly IFileOperationQueueService _fileOperationQueueService;
    private readonly Stack<HistoryOperation> _undoStack = [];
    private readonly Stack<HistoryOperation> _redoStack = [];
    private bool _isApplyingHistory;
    private readonly string _undoStagingPath;

    [ObservableProperty]
    private string _currentPath = string.Empty;

    [ObservableProperty]
    private bool _showHiddenFiles;

    [ObservableProperty]
    private FileSystemItem? _selectedItem;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _transferSummary = string.Empty;

    [ObservableProperty]
    private int _itemCount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UndoCommand))]
    private bool _canUndo;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RedoCommand))]
    private bool _canRedo;

    [ObservableProperty]
    private string _undoDescription = "Undo";

    [ObservableProperty]
    private string _redoDescription = "Redo";

    [ObservableProperty]
    private string _viewMode = "Details";

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private NameMatchMode _filterMatchMode = NameMatchMode.Contains;

    [ObservableProperty]
    private bool _clearFilterOnFolderChange;

    [ObservableProperty]
    private bool _isSearchVisible;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private NameMatchMode _searchMatchMode = NameMatchMode.Contains;

    [ObservableProperty]
    private bool _isShowingSearchResults;

    [ObservableProperty]
    private string _searchResultsText = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    public bool IsFileOperationQueueBusy => _fileOperationQueueService.IsBusy;
    public string FileOperationQueueStatus => _fileOperationQueueService.StatusText;
    public ObservableCollection<FileSystemItem> Items { get; } = [];
    public ObservableCollection<FileSystemItem> SelectedItems { get; } = [];
    public ObservableCollection<string> FilterHistory { get; } = [];
    public IReadOnlyList<NameMatchMode> AvailableMatchModes { get; } = Enum.GetValues<NameMatchMode>();

    public event EventHandler<string>? NavigateRequested;
    public event EventHandler? SelectAllRequested;
    public event EventHandler? InvertSelectionRequested;
    public event EventHandler<FileSystemItem>? InlineRenameRequested;
    public event EventHandler<string>? ViewModeChanged;
    public event EventHandler<FileSystemItem>? PropertiesRequested;
    public event EventHandler? SearchPanelOpened;
    public event EventHandler<ArchiveBrowseRequest>? ArchiveBrowseRequested;
    public event EventHandler<string>? FilterHistoryEntryAdded;

    [RelayCommand]
    private void InvertSelection()
    {
        InvertSelectionRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void AddCurrentFilterToHistory()
    {
        AddFilterHistoryEntry(FilterText);
    }

    [RelayCommand]
    private async Task Duplicate()
    {
        var paths = GetSelectedPaths();
        if (paths.Count == 0) return;

        var transferResults = await TransferFilesAsync(
            paths,
            CurrentPath,
            moveFiles: false,
            "Duplicate Error",
            FileTransferCollisionResolution.Rename);

        if (transferResults.Any())
        {
            var successfulTransfers = GetSuccessfulTransfers(transferResults);
            RegisterCopyUndo(successfulTransfers);
        }
    }

    [RelayCommand]
    private void CopyPath()
    {
        var item = GetSingleSelectedItem();
        if (item == null) return;
        Clipboard.SetText(item.FullPath);
    }

    [RelayCommand]
    private void CopyName()
    {
        var item = GetSingleSelectedItem();
        if (item == null) return;
        Clipboard.SetText(item.Name);
    }

    [RelayCommand]
    private void NewFile()
    {
        if (string.IsNullOrWhiteSpace(CurrentPath)) return;

        var dialog = new NewFolderDialog { Title = "New File", Message = "Enter file name:" };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var createdPath = _fileSystemService.CreateFile(CurrentPath, dialog.FolderName);
            var parentPath = Path.GetDirectoryName(createdPath) ?? CurrentPath;
            var fileName = Path.GetFileName(createdPath);
            PushHistory(
                "Undo New File",
                () => _fileSystemService.DeleteFiles([createdPath], permanentDelete: true),
                "Redo New File",
                () => _fileSystemService.CreateFile(parentPath, fileName));
            Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Create file failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void CreateSymbolicLink()
    {
        var item = GetSingleSelectedItem();
        if (item == null) return;

        var dialog = new NewFolderDialog { Title = "Create Symbolic Link", Message = "Enter link name:", FolderName = item.Name + " - Link" };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var linkPath = Path.Combine(CurrentPath, dialog.FolderName);
            _fileSystemService.CreateSymbolicLink(linkPath, item.FullPath);
            PushHistory(
                "Undo Create Link",
                () => _fileSystemService.DeleteFiles([linkPath], permanentDelete: true),
                "Redo Create Link",
                () => _fileSystemService.CreateSymbolicLink(linkPath, item.FullPath));
            Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Create link failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public FileListViewModel(
        IFileSystemService fileSystemService,
        IClipboardService clipboardService,
        IFileOperationQueueService? fileOperationQueueService = null)
    {
        _fileSystemService = fileSystemService;
        _clipboardService = clipboardService;
        _fileOperationQueueService = fileOperationQueueService ?? new FileOperationQueueService();
        _undoStagingPath = Path.Combine(
            Path.GetTempPath(),
            "CubicAIExplorer",
            "UndoStaging",
            Environment.ProcessId.ToString());
        Directory.CreateDirectory(_undoStagingPath);
        _fileOperationQueueService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IFileOperationQueueService.IsBusy))
                OnPropertyChanged(nameof(IsFileOperationQueueBusy));
            if (e.PropertyName == nameof(IFileOperationQueueService.StatusText)
                || e.PropertyName == nameof(IFileOperationQueueService.IsBusy)
                || e.PropertyName == nameof(IFileOperationQueueService.PendingCount)
                || e.PropertyName == nameof(IFileOperationQueueService.CurrentOperationText))
                OnPropertyChanged(nameof(FileOperationQueueStatus));
        };
    }

    public void LoadDirectory(string path)
    {
        if (!_fileSystemService.DirectoryExists(path)) return;

        var pathChanged = !string.Equals(CurrentPath, path, StringComparison.OrdinalIgnoreCase);
        if (pathChanged && ClearFilterOnFolderChange && !string.IsNullOrWhiteSpace(FilterText))
            FilterText = string.Empty;

        CurrentPath = path;
        Items.Clear();

        var items = _fileSystemService.GetDirectoryContents(path, ShowHiddenFiles);
        _allItems = items
            .OrderByDescending(i => i.ItemType == FileSystemItemType.Directory)
            .ThenBy(i => i.Name)
            .ToList();

        ApplyFilter();
    }

    [RelayCommand]
    private void OpenItem(FileSystemItem? item)
    {
        if (item == null) return;

        switch (item.ItemType)
        {
            case FileSystemItemType.Directory:
            case FileSystemItemType.Drive:
                NavigateRequested?.Invoke(this, item.FullPath);
                break;
            case FileSystemItemType.File:
                if (IsArchiveItem(item))
                {
                    BrowseArchive(item);
                }
                else
                {
                    _fileSystemService.OpenFile(item.FullPath);
                }
                break;
        }
    }

    [RelayCommand]
    private void NavigateUp()
    {
        var parent = _fileSystemService.GetParentPath(CurrentPath);
        if (parent != CurrentPath)
        {
            NavigateRequested?.Invoke(this, parent);
        }
    }

    [RelayCommand]
    private void Copy()
    {
        var paths = GetSelectedPaths();
        if (paths.Count == 0) return;

        _clipboardService.SetFiles(paths, isCut: false);
    }

    [RelayCommand]
    private void Cut()
    {
        var paths = GetSelectedPaths();
        if (paths.Count == 0) return;

        _clipboardService.SetFiles(paths, isCut: true);
    }

    [RelayCommand]
    private async Task Paste()
    {
        if (string.IsNullOrWhiteSpace(CurrentPath) || !_fileSystemService.DirectoryExists(CurrentPath))
            return;

        var (paths, isCut) = _clipboardService.GetFiles();
        if (paths.Count == 0) return;

        var collisionResolution = ResolvePasteCollisionResolution(paths, CurrentPath);
        if (collisionResolution == null)
            return;

        var transferResults = await TransferFilesAsync(
                paths,
                CurrentPath,
                moveFiles: isCut,
                "Paste Error",
                collisionResolution.Value);
        if (transferResults.Count > 0)
        {
            var successfulTransfers = GetSuccessfulTransfers(transferResults);

            if (isCut)
            {
                RegisterMoveUndo(successfulTransfers);
                if (transferResults.All(static result => result.Status == FileTransferStatus.Success))
                    _clipboardService.Clear();
            }
            else
            {
                RegisterCopyUndo(successfulTransfers);
            }
        }
    }

    [RelayCommand]
    private Task Delete()
    {
        return DeleteSelectedAsync(permanentDelete: false);
    }

    [RelayCommand]
    private Task PermanentDelete()
    {
        return DeleteSelectedAsync(permanentDelete: true);
    }

    [RelayCommand]
    private void Rename()
    {
        var item = GetSingleSelectedItem();
        if (item == null) return;
        InlineRenameRequested?.Invoke(this, item);
    }

    [RelayCommand]
    private void NewFolder()
    {
        if (string.IsNullOrWhiteSpace(CurrentPath)) return;

        var dialog = new NewFolderDialog();
        if (dialog.ShowDialog() != true) return;

        try
        {
            var createdPath = _fileSystemService.CreateFolder(CurrentPath, dialog.FolderName);
            var parentPath = Path.GetDirectoryName(createdPath) ?? CurrentPath;
            var folderName = Path.GetFileName(createdPath);
            PushHistory(
                "Undo New Folder",
                () => _fileSystemService.DeleteFiles([createdPath], permanentDelete: true),
                "Redo New Folder",
                () => _fileSystemService.CreateFolder(parentPath, folderName));
            Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Create folder failed: {ex.Message}",
                "Create Folder Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ShowProperties()
    {
        var item = GetSingleSelectedItem();
        if (item == null) return;
        PropertiesRequested?.Invoke(this, item);
    }

    [RelayCommand]
    private async Task ExtractArchive()
    {
        var item = GetSingleSelectedItem();
        if (!IsArchiveItem(item))
            return;

        var defaultDestination = GetDefaultExtractDestination(item!);
        var dialog = new ExtractArchiveDialog(item!.Name, defaultDestination);
        if (dialog.ShowDialog() != true)
            return;

        await ExtractArchiveToAsync(item, dialog.DestinationPath, dialog.OpenFolderWhenDone);
    }

    [RelayCommand]
    private void BrowseArchive()
    {
        var item = GetSingleSelectedItem();
        if (!IsArchiveItem(item))
            return;

        BrowseArchive(item!);
    }

    private void BrowseArchive(FileSystemItem item)
    {
        try
        {
            var entries = _fileSystemService.GetArchiveEntries(item.FullPath, maxEntries: int.MaxValue);
            ArchiveBrowseRequested?.Invoke(this, new ArchiveBrowseRequest(item.FullPath, entries, this));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Browse archive failed: {ex.Message}",
                "Archive Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        if (!string.IsNullOrWhiteSpace(CurrentPath))
            LoadDirectory(CurrentPath);
    }

    [RelayCommand]
    private void SelectAll()
    {
        SelectAllRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (_undoStack.Count == 0) return;

        var operation = _undoStack.Pop();

        try
        {
            _isApplyingHistory = true;
            operation.UndoAction();
            _redoStack.Push(operation);
            Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Undo failed: {ex.Message}",
                "Undo Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _isApplyingHistory = false;
            UpdateHistoryState();
        }
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (_redoStack.Count == 0) return;

        var operation = _redoStack.Pop();

        try
        {
            _isApplyingHistory = true;
            operation.RedoAction();
            _undoStack.Push(operation);
            Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Redo failed: {ex.Message}",
                "Redo Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _isApplyingHistory = false;
            UpdateHistoryState();
        }
    }

    [RelayCommand]
    private void ClearHistory()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        UpdateHistoryState();
    }

    [RelayCommand]
    private void SearchInFolder()
    {
        IsSearchVisible = true;
        SearchPanelOpened?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void CloseSearch()
    {
        IsSearchVisible = false;
        SearchText = string.Empty;
        if (IsShowingSearchResults)
            ClearSearchResults();
    }

    [RelayCommand]
    private async Task ExecuteSearch()
    {
        if (string.IsNullOrWhiteSpace(SearchText) || string.IsNullOrWhiteSpace(CurrentPath))
            return;

        IsSearching = true;
        var searchTerm = SearchText;
        var searchPath = CurrentPath;
        var searchMode = SearchMatchMode;

        try
        {
            var results = await Task.Run(() =>
                SearchFilesRecursive(searchPath, searchTerm, searchMode, ShowHiddenFiles));

            ApplySearchResults(results, searchTerm);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Search failed: {ex.Message}",
                "Search Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsSearching = false;
        }
    }

    public void ExecuteSearchSync()
    {
        if (string.IsNullOrWhiteSpace(SearchText) || string.IsNullOrWhiteSpace(CurrentPath))
            return;

        var results = SearchFilesRecursive(CurrentPath, SearchText, SearchMatchMode, ShowHiddenFiles);
        ApplySearchResults(results, SearchText);
    }

    public void ApplySavedSearch(string searchPath, string searchTerm, NameMatchMode matchMode = NameMatchMode.Contains)
    {
        if (string.IsNullOrWhiteSpace(searchPath) || string.IsNullOrWhiteSpace(searchTerm))
            return;

        if (!string.Equals(CurrentPath, searchPath, StringComparison.OrdinalIgnoreCase))
            LoadDirectory(searchPath);

        SearchMatchMode = matchMode;
        SearchText = searchTerm;
        IsSearchVisible = true;
        ExecuteSearchSync();
    }

    public void SetFilterHistory(IEnumerable<string> entries)
    {
        FilterHistory.Clear();
        foreach (var entry in entries
                     .Where(static entry => !string.IsNullOrWhiteSpace(entry))
                     .Select(static entry => entry.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            FilterHistory.Add(entry);
        }
    }

    public static bool IsArchiveItem(FileSystemItem? item)
        => item != null
           && item.ItemType == FileSystemItemType.File
           && string.Equals(item.Extension, ".zip", StringComparison.OrdinalIgnoreCase);

    public static string GetDefaultExtractDestination(FileSystemItem item)
    {
        var parentPath = Path.GetDirectoryName(item.FullPath) ?? item.FullPath;
        return Path.Combine(parentPath, Path.GetFileNameWithoutExtension(item.Name));
    }

    public async Task ExtractArchiveToAsync(FileSystemItem item, string destinationPath, bool openFolderWhenDone = false)
    {
        if (!IsArchiveItem(item) || string.IsNullOrWhiteSpace(destinationPath))
            return;

        try
        {
            var sanitizedDestination = _fileSystemService.EnsureDirectoryExists(destinationPath);
            if (string.IsNullOrWhiteSpace(sanitizedDestination))
                throw new InvalidOperationException("The extraction destination is invalid.");

            await _fileOperationQueueService.EnqueueAsync(
                $"Extracting {item.Name}",
                context =>
                {
                    _fileSystemService.ExtractArchive(item.FullPath, sanitizedDestination, context);
                    return true;
                });

            SetTransferSummary($"Extracted archive to {Path.GetFileName(sanitizedDestination)}");
            Refresh();

            if (openFolderWhenDone)
                _fileSystemService.OpenInDefaultApp(sanitizedDestination);
        }
        catch (OperationCanceledException)
        {
            SetTransferSummary($"Canceled archive extraction for {item.Name}");
            Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Extract archive failed: {ex.Message}",
                "Archive Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public async Task ExtractArchiveEntriesToAsync(
        string archivePath,
        IEnumerable<string> entryPaths,
        string destinationPath,
        bool openFolderWhenDone = false)
    {
        if (string.IsNullOrWhiteSpace(archivePath) || string.IsNullOrWhiteSpace(destinationPath))
            return;

        var entryPathList = entryPaths
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (entryPathList.Length == 0)
            return;

        try
        {
            var sanitizedDestination = _fileSystemService.EnsureDirectoryExists(destinationPath);
            if (string.IsNullOrWhiteSpace(sanitizedDestination))
                throw new InvalidOperationException("The extraction destination is invalid.");

            await _fileOperationQueueService.EnqueueAsync(
                $"Extracting {entryPathList.Length} archive item(s)",
                context =>
                {
                    _fileSystemService.ExtractArchiveEntries(archivePath, sanitizedDestination, entryPathList, context);
                    return true;
                });

            SetTransferSummary($"Extracted {entryPathList.Length} archive item(s)");
            Refresh();

            if (openFolderWhenDone)
                _fileSystemService.OpenInDefaultApp(sanitizedDestination);
        }
        catch (OperationCanceledException)
        {
            SetTransferSummary("Archive extraction canceled");
            Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Extract archive failed: {ex.Message}",
                "Archive Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ApplySearchResults(List<FileSystemItem> results, string searchTerm)
    {
        Items.Clear();
        _allItems = results;
        foreach (var item in results)
            Items.Add(item);

        ItemCount = Items.Count;
        IsShowingSearchResults = true;
        SearchResultsText = $"Search results for \"{searchTerm}\" — {results.Count} item(s) found";
        UpdateSelectionStatus();
    }

    [RelayCommand]
    private void ClearSearchResults()
    {
        IsShowingSearchResults = false;
        SearchResultsText = string.Empty;
        LoadDirectory(CurrentPath);
    }

    private List<FileSystemItem> SearchFilesRecursive(string rootPath, string searchTerm, NameMatchMode matchMode, bool showHidden)
    {
        var results = new List<FileSystemItem>();
        var stack = new Stack<string>();
        stack.Push(rootPath);

        while (stack.Count > 0)
        {
            var currentDir = stack.Pop();
            try
            {
                var dirInfo = new DirectoryInfo(currentDir);
                if (!dirInfo.Exists) continue;

                foreach (var file in dirInfo.EnumerateFiles())
                {
                    if (!showHidden && file.Attributes.HasFlag(FileAttributes.Hidden)) continue;
                    if (file.Attributes.HasFlag(FileAttributes.System)) continue;

                    if (IsNameMatch(file.Name, searchTerm, matchMode))
                    {
                        results.Add(new FileSystemItem
                        {
                            Name = file.Name,
                            FullPath = file.FullName,
                            ItemType = FileSystemItemType.File,
                            Size = file.Length,
                            Extension = file.Extension,
                            DateModified = file.LastWriteTime,
                            DateCreated = file.CreationTime,
                            ShellTypeName = ShellFileInfoHelper.TryGetTypeName(file.FullName, FileSystemItemType.File) ?? string.Empty,
                            Attributes = file.Attributes
                        });
                    }
                }

                foreach (var dir in dirInfo.EnumerateDirectories())
                {
                    if (!showHidden && dir.Attributes.HasFlag(FileAttributes.Hidden)) continue;
                    if (dir.Attributes.HasFlag(FileAttributes.System)) continue;

                    if (IsNameMatch(dir.Name, searchTerm, matchMode))
                    {
                        results.Add(new FileSystemItem
                        {
                            Name = dir.Name,
                            FullPath = dir.FullName,
                            ItemType = FileSystemItemType.Directory,
                            DateModified = dir.LastWriteTime,
                            DateCreated = dir.CreationTime,
                            ShellTypeName = ShellFileInfoHelper.TryGetTypeName(dir.FullName, FileSystemItemType.Directory) ?? string.Empty,
                            Attributes = dir.Attributes
                        });
                    }

                    stack.Push(dir.FullName);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }

        return results
            .OrderByDescending(i => i.ItemType == FileSystemItemType.Directory)
            .ThenBy(i => i.Name)
            .ToList();
    }

    partial void OnShowHiddenFilesChanged(bool value)
    {
        if (!string.IsNullOrEmpty(CurrentPath))
            LoadDirectory(CurrentPath);
    }

    partial void OnViewModeChanged(string value)
    {
        ViewModeChanged?.Invoke(this, value);
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter();
    }

    private List<FileSystemItem> _allItems = [];

    private void ApplyFilter()
    {
        Items.Clear();
        var filtered = string.IsNullOrWhiteSpace(FilterText)
            ? _allItems
            : _allItems.Where(i => IsNameMatch(i.Name, FilterText, FilterMatchMode)).ToList();

        foreach (var item in filtered)
            Items.Add(item);

        ItemCount = Items.Count;
        UpdateSelectionStatus();
    }

    public void UpdateSelectionStatus()
    {
        StatusText = BuildSelectionStatusText();
    }

    private string BuildSelectionStatusText()
    {
        var selectionCount = SelectedItems.Count;
        string baseStatus;
        if (selectionCount > 0)
        {
            var totalSize = SelectedItems
                .Where(i => i.ItemType == FileSystemItemType.File)
                .Sum(i => i.Size);
            var sizeText = totalSize > 0 ? $" ({FormatSize(totalSize)})" : "";
            baseStatus = $"{ItemCount} items | {selectionCount} selected{sizeText}";
        }
        else
        {
            baseStatus = $"{ItemCount} items";
        }

        return string.IsNullOrWhiteSpace(TransferSummary)
            ? baseStatus
            : $"{TransferSummary} | {baseStatus}";
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };

    private void AddFilterHistoryEntry(string? entry)
    {
        var normalized = entry?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
            return;

        for (var i = FilterHistory.Count - 1; i >= 0; i--)
        {
            if (string.Equals(FilterHistory[i], normalized, StringComparison.OrdinalIgnoreCase))
                FilterHistory.RemoveAt(i);
        }

        FilterHistory.Insert(0, normalized);
        while (FilterHistory.Count > 15)
            FilterHistory.RemoveAt(FilterHistory.Count - 1);

        FilterHistoryEntryAdded?.Invoke(this, normalized);
    }

    private static bool IsNameMatch(string candidate, string pattern, NameMatchMode mode)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return true;

        return mode switch
        {
            NameMatchMode.Exact => string.Equals(candidate, pattern, StringComparison.OrdinalIgnoreCase),
            NameMatchMode.Wildcard => IsWildcardMatch(candidate, pattern),
            _ => candidate.Contains(pattern, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static bool IsWildcardMatch(string candidate, string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern.Trim())
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return Regex.IsMatch(candidate, regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private List<string> GetSelectedPaths()
    {
        var selected = SelectedItems.Count > 0
            ? SelectedItems.Select(static item => item.FullPath)
            : SelectedItem != null
                ? [SelectedItem.FullPath]
                : [];

        return selected
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private FileSystemItem? GetSingleSelectedItem()
    {
        if (SelectedItems.Count == 1)
            return SelectedItems[0];

        return SelectedItems.Count == 0 ? SelectedItem : null;
    }

    private Task DeleteSelectedAsync(bool permanentDelete)
    {
        return DeletePathsAsync(GetSelectedPaths(), permanentDelete, promptUser: true);
    }

    public void RenameItem(FileSystemItem item, string newName)
    {
        if (item == null || string.IsNullOrWhiteSpace(newName))
            return;

        try
        {
            var previousPath = item.FullPath;
            var renamedPath = _fileSystemService.RenameFile(previousPath, newName);
            var previousName = item.Name;
            var nextName = Path.GetFileName(renamedPath);
            PushHistory(
                "Undo Rename",
                () => _fileSystemService.RenameFile(renamedPath, previousName),
                "Redo Rename",
                () => _fileSystemService.RenameFile(previousPath, nextName));
            Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Rename failed: {ex.Message}",
                "Rename Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public void ImportDroppedFiles(IEnumerable<string> paths, string destinationPath, bool moveFiles)
    {
        ImportDroppedFilesAsync(paths, destinationPath, moveFiles).GetAwaiter().GetResult();
    }

    public async Task ImportDroppedFilesAsync(IEnumerable<string> paths, string destinationPath, bool moveFiles)
    {
        var transferResults = await TransferFilesAsync(
                paths,
                destinationPath,
                moveFiles,
                "Drop Error",
                FileTransferCollisionResolution.KeepBoth);
        if (transferResults.Count == 0)
            return;

        var successfulTransfers = GetSuccessfulTransfers(transferResults);

        if (moveFiles)
        {
            RegisterMoveUndo(successfulTransfers);
        }
        else
        {
            RegisterCopyUndo(successfulTransfers);
        }
    }

    public IReadOnlyList<string> GetSelectedPathsForTransfer() => GetSelectedPaths();

    private async Task<IReadOnlyList<FileTransferResult>> TransferFilesAsync(
        IEnumerable<string> sourcePaths,
        string destinationPath,
        bool moveFiles,
        string errorTitle,
        FileTransferCollisionResolution collisionResolution)
    {
        if (string.IsNullOrWhiteSpace(destinationPath) || !_fileSystemService.DirectoryExists(destinationPath))
            return [];

        var distinctPaths = sourcePaths
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (distinctPaths.Length == 0)
            return [];

        try
        {
            var operationText = $"{(moveFiles ? "Moving" : "Copying")} {distinctPaths.Length} item(s)";
            var transferResults = await _fileOperationQueueService.EnqueueAsync(
                operationText,
                context => moveFiles
                    ? _fileSystemService.MoveFiles(distinctPaths, destinationPath, collisionResolution, context)
                    : _fileSystemService.CopyFiles(distinctPaths, destinationPath, collisionResolution, context));

            SetTransferSummary(BuildTransferSummary(moveFiles ? "Moved" : "Copied", transferResults));
            ShowTransferIssues(transferResults, errorTitle);
            Refresh();
            return transferResults.Any(static result => result.Status == FileTransferStatus.Success)
                ? transferResults
                : [];
        }
        catch (OperationCanceledException)
        {
            SetTransferSummary($"{(moveFiles ? "Move" : "Copy")} canceled");
            Refresh();
            return [];
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Operation failed: {ex.Message}",
                errorTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return [];
        }
    }

    private FileTransferCollisionResolution? ResolvePasteCollisionResolution(IEnumerable<string> sourcePaths, string destinationPath)
    {
        var conflictCount = CountConflictingTargets(sourcePaths, destinationPath);
        if (conflictCount == 0)
            return FileTransferCollisionResolution.KeepBoth;

        var dialog = new FileConflictDialog(conflictCount);
        if (dialog.ShowDialog() != true)
            return null;

        return dialog.Resolution;
    }

    private int CountConflictingTargets(IEnumerable<string> sourcePaths, string destinationPath)
    {
        var count = 0;

        foreach (var sourcePath in sourcePaths
                     .Where(static path => !string.IsNullOrWhiteSpace(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var entryName = GetTransferEntryName(sourcePath);
            if (string.IsNullOrWhiteSpace(entryName))
                continue;

            var targetPath = Path.Combine(destinationPath, entryName);
            if (_fileSystemService.FileExists(targetPath) || _fileSystemService.DirectoryExists(targetPath))
                count++;
        }

        return count;
    }

    private static string GetTransferEntryName(string sourcePath)
    {
        var normalizedPath = sourcePath.TrimEnd('\\');
        return Path.GetFileName(normalizedPath);
    }

    private void SetTransferSummary(string summary)
    {
        TransferSummary = summary;
        UpdateSelectionStatus();
    }

    private static string BuildTransferSummary(string actionVerb, IReadOnlyList<FileTransferResult> transferResults)
    {
        var successCount = transferResults.Count(static result => result.Status == FileTransferStatus.Success);
        var skippedCount = transferResults.Count(static result => result.Status == FileTransferStatus.Skipped);
        var failedCount = transferResults.Count(static result => result.Status == FileTransferStatus.Failed);

        var parts = new List<string>();
        if (successCount > 0)
            parts.Add($"{actionVerb} {successCount} item(s)");
        if (skippedCount > 0)
            parts.Add($"Skipped {skippedCount}");
        if (failedCount > 0)
            parts.Add($"Failed {failedCount}");

        return parts.Count == 0 ? string.Empty : string.Join(" | ", parts);
    }

    private static void ShowTransferIssues(IReadOnlyList<FileTransferResult> transferResults, string errorTitle)
    {
        var failedItems = transferResults
            .Where(static result => result.Status == FileTransferStatus.Failed)
            .ToArray();

        if (failedItems.Length == 0)
            return;

        var skippedCount = transferResults.Count(static result => result.Status == FileTransferStatus.Skipped);
        var message = $"Completed with {failedItems.Length} failure(s) and {skippedCount} skipped item(s).";

        var firstFailure = failedItems[0];
        var details = string.IsNullOrWhiteSpace(firstFailure.ErrorMessage)
            ? Path.GetFileName(firstFailure.SourcePath)
            : $"{Path.GetFileName(firstFailure.SourcePath)}: {firstFailure.ErrorMessage}";
        message = $"{message}\n\nFirst failure: {details}";

        MessageBox.Show(
            message,
            errorTitle,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private static FileTransferResult[] GetSuccessfulTransfers(IReadOnlyList<FileTransferResult> transferResults)
    {
        return transferResults
            .Where(static result => result.Status == FileTransferStatus.Success)
            .ToArray();
    }

    private void RegisterMoveUndo(IReadOnlyList<FileTransferResult> results)
    {
        if (_isApplyingHistory || results.Count == 0) return;

        var undoData = results.ToArray();
        PushHistory(
            "Undo Move",
            () =>
            {
                foreach (var item in undoData)
                {
                    var destinationDir = Path.GetDirectoryName(item.SourcePath);
                    if (string.IsNullOrWhiteSpace(destinationDir)) continue;
                    _fileSystemService.MoveFiles([item.DestinationPath], destinationDir);
                }
            },
            "Redo Move",
            () =>
            {
                foreach (var item in undoData)
                {
                    var destinationDir = Path.GetDirectoryName(item.DestinationPath);
                    if (string.IsNullOrWhiteSpace(destinationDir)) continue;
                    _fileSystemService.MoveFiles([item.SourcePath], destinationDir);
                }
            });
    }

    private void RegisterCopyUndo(IReadOnlyList<FileTransferResult> results)
    {
        if (_isApplyingHistory || results.Count == 0) return;

        var copiedPaths = results.Select(static x => x.DestinationPath).ToArray();
        var sourcePaths = results.Select(static x => x.SourcePath).ToArray();
        var destinationDirectories = results
            .Select(static x => Path.GetDirectoryName(x.DestinationPath))
            .ToArray();

        PushHistory(
            "Undo Copy",
            () => _fileSystemService.DeleteFiles(copiedPaths, permanentDelete: true),
            "Redo Copy",
            () =>
            {
                for (var i = 0; i < sourcePaths.Length; i++)
                {
                    var destinationDir = destinationDirectories[i];
                    if (string.IsNullOrWhiteSpace(destinationDir)) continue;
                    _fileSystemService.CopyFiles([sourcePaths[i]], destinationDir);
                }
            });
    }

    private void RegisterPermanentDeleteUndo(IReadOnlyList<FileTransferResult> stagedItems)
    {
        if (_isApplyingHistory || stagedItems.Count == 0) return;

        var restoreData = stagedItems.ToArray();
        PushHistory(
            "Undo Permanent Delete",
            () =>
            {
                foreach (var item in restoreData)
                {
                    var restoreDir = Path.GetDirectoryName(item.SourcePath);
                    if (string.IsNullOrWhiteSpace(restoreDir)) continue;

                    var movedBack = GetSuccessfulTransfers(_fileSystemService.MoveFiles([item.DestinationPath], restoreDir));
                    if (movedBack.Length == 0) continue;

                    var restoredPath = movedBack[0].DestinationPath;
                    if (!string.Equals(restoredPath, item.SourcePath, StringComparison.OrdinalIgnoreCase))
                    {
                        var originalName = Path.GetFileName(item.SourcePath);
                        _fileSystemService.RenameFile(restoredPath, originalName);
                    }
                }
            },
            "Redo Permanent Delete",
            () =>
            {
                foreach (var item in restoreData)
                {
                    _fileSystemService.MoveFiles([item.SourcePath], _undoStagingPath);
                }
            });
    }

    public void DeletePaths(IEnumerable<string> paths, bool permanentDelete, bool promptUser)
    {
        DeletePathsAsync(paths, permanentDelete, promptUser).GetAwaiter().GetResult();
    }

    public async Task DeletePathsAsync(IEnumerable<string> paths, bool permanentDelete, bool promptUser)
    {
        var deletePaths = paths
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (deletePaths.Length == 0) return;

        if (promptUser)
        {
            var confirmationText = permanentDelete
                ? "Permanently delete selected item(s)? This cannot be undone."
                : "Delete selected item(s) to the Recycle Bin?";

            var result = MessageBox.Show(
                confirmationText,
                permanentDelete ? "Permanent Delete" : "Delete",
                MessageBoxButton.YesNo,
                permanentDelete ? MessageBoxImage.Warning : MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;
        }

        try
        {
            if (permanentDelete)
            {
                var stagedItems = await _fileOperationQueueService.EnqueueAsync(
                    $"Permanently deleting {deletePaths.Length} item(s)",
                    context => _fileSystemService.MoveFiles(deletePaths, _undoStagingPath, operationContext: context));
                SetTransferSummary(BuildTransferSummary("Permanently deleted", stagedItems));
                ShowTransferIssues(stagedItems, "Delete Error");
                RegisterPermanentDeleteUndo(GetSuccessfulTransfers(stagedItems));
            }
            else
            {
                await _fileOperationQueueService.EnqueueAsync(
                    $"Deleting {deletePaths.Length} item(s)",
                    context =>
                    {
                        _fileSystemService.DeleteFiles(deletePaths, permanentDelete: false, context);
                        return true;
                    });
                SetTransferSummary($"Deleted {deletePaths.Length} item(s)");
            }

            Refresh();
        }
        catch (OperationCanceledException)
        {
            SetTransferSummary("Delete canceled");
            Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Delete failed: {ex.Message}",
                "Delete Error",
                MessageBoxButton.OK,
            MessageBoxImage.Error);
        }
    }

    private void PushHistory(string undoDescription, Action undoAction, string redoDescription, Action redoAction)
    {
        if (_isApplyingHistory) return;

        _undoStack.Push(new HistoryOperation(undoDescription, undoAction, redoDescription, redoAction));
        _redoStack.Clear();
        UpdateHistoryState();
    }

    private void UpdateHistoryState()
    {
        CanUndo = _undoStack.Count > 0;
        CanRedo = _redoStack.Count > 0;
        UndoDescription = _undoStack.Count > 0 ? _undoStack.Peek().UndoDescription : "Undo";
        RedoDescription = _redoStack.Count > 0 ? _redoStack.Peek().RedoDescription : "Redo";
    }

    private sealed record HistoryOperation(
        string UndoDescription,
        Action UndoAction,
        string RedoDescription,
        Action RedoAction);
}
