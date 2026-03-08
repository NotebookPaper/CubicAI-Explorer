using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
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
    private readonly Stack<UndoOperation> _undoStack = [];
    private bool _isApplyingUndo;

    [ObservableProperty]
    private string _currentPath = string.Empty;

    [ObservableProperty]
    private bool _showHiddenFiles;

    [ObservableProperty]
    private FileSystemItem? _selectedItem;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private int _itemCount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UndoCommand))]
    private bool _canUndo;

    public ObservableCollection<FileSystemItem> Items { get; } = [];
    public ObservableCollection<FileSystemItem> SelectedItems { get; } = [];

    public event EventHandler<string>? NavigateRequested;
    public event EventHandler? SelectAllRequested;
    public event EventHandler<FileSystemItem>? InlineRenameRequested;

    public FileListViewModel(IFileSystemService fileSystemService, IClipboardService clipboardService)
    {
        _fileSystemService = fileSystemService;
        _clipboardService = clipboardService;
    }

    public void LoadDirectory(string path)
    {
        if (!_fileSystemService.DirectoryExists(path)) return;

        CurrentPath = path;
        Items.Clear();

        var items = _fileSystemService.GetDirectoryContents(path, ShowHiddenFiles);
        foreach (var item in items.OrderByDescending(i => i.ItemType == FileSystemItemType.Directory).ThenBy(i => i.Name))
        {
            Items.Add(item);
        }

        ItemCount = Items.Count;
        StatusText = $"{ItemCount} items";
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
                _fileSystemService.OpenFile(item.FullPath);
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
    private void Paste()
    {
        if (string.IsNullOrWhiteSpace(CurrentPath) || !_fileSystemService.DirectoryExists(CurrentPath))
            return;

        var (paths, isCut) = _clipboardService.GetFiles();
        if (paths.Count == 0) return;

        if (TransferFiles(paths, CurrentPath, moveFiles: isCut, "Paste Error", out var transferResults))
        {
            if (isCut)
            {
                RegisterMoveUndo(transferResults);
                _clipboardService.Clear();
            }
        }
    }

    [RelayCommand]
    private void Delete()
    {
        DeleteSelected(permanentDelete: false);
    }

    [RelayCommand]
    private void PermanentDelete()
    {
        DeleteSelected(permanentDelete: true);
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
            PushUndo(
                "Undo New Folder",
                () => _fileSystemService.DeleteFiles([createdPath], permanentDelete: true));
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
        UpdateUndoState();

        try
        {
            _isApplyingUndo = true;
            operation.Apply();
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
            _isApplyingUndo = false;
        }
    }

    partial void OnShowHiddenFilesChanged(bool value)
    {
        if (!string.IsNullOrEmpty(CurrentPath))
            LoadDirectory(CurrentPath);
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

    private void DeleteSelected(bool permanentDelete)
    {
        var paths = GetSelectedPaths();
        if (paths.Count == 0) return;

        var confirmationText = permanentDelete
            ? "Permanently delete selected item(s)? This cannot be undone."
            : "Delete selected item(s) to the Recycle Bin?";

        var result = MessageBox.Show(
            confirmationText,
            permanentDelete ? "Permanent Delete" : "Delete",
            MessageBoxButton.YesNo,
            permanentDelete ? MessageBoxImage.Warning : MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            _fileSystemService.DeleteFiles(paths, permanentDelete);
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

    public void RenameItem(FileSystemItem item, string newName)
    {
        if (item == null || string.IsNullOrWhiteSpace(newName))
            return;

        try
        {
            var previousPath = item.FullPath;
            var renamedPath = _fileSystemService.RenameFile(previousPath, newName);
            var previousName = item.Name;
            PushUndo(
                "Undo Rename",
                () => _fileSystemService.RenameFile(renamedPath, previousName));
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
        if (TransferFiles(paths, destinationPath, moveFiles, "Drop Error", out var transferResults) && moveFiles)
        {
            RegisterMoveUndo(transferResults);
        }
    }

    public IReadOnlyList<string> GetSelectedPathsForTransfer() => GetSelectedPaths();

    private bool TransferFiles(
        IEnumerable<string> sourcePaths,
        string destinationPath,
        bool moveFiles,
        string errorTitle,
        out IReadOnlyList<FileTransferResult> transferResults)
    {
        transferResults = [];
        if (string.IsNullOrWhiteSpace(destinationPath) || !_fileSystemService.DirectoryExists(destinationPath))
            return false;

        var distinctPaths = sourcePaths
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (distinctPaths.Length == 0)
            return false;

        try
        {
            if (moveFiles)
            {
                transferResults = _fileSystemService.MoveFiles(distinctPaths, destinationPath);
            }
            else
            {
                transferResults = _fileSystemService.CopyFiles(distinctPaths, destinationPath);
            }

            Refresh();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Operation failed: {ex.Message}",
                errorTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }

    private void RegisterMoveUndo(IReadOnlyList<FileTransferResult> results)
    {
        if (_isApplyingUndo || results.Count == 0) return;

        var undoData = results.ToArray();
        PushUndo(
            "Undo Move",
            () =>
            {
                foreach (var item in undoData)
                {
                    var destinationDir = Path.GetDirectoryName(item.SourcePath);
                    if (string.IsNullOrWhiteSpace(destinationDir)) continue;
                    _fileSystemService.MoveFiles([item.DestinationPath], destinationDir);
                }
            });
    }

    private void PushUndo(string description, Action apply)
    {
        if (_isApplyingUndo) return;

        _undoStack.Push(new UndoOperation(description, apply));
        UpdateUndoState();
    }

    private void UpdateUndoState()
    {
        CanUndo = _undoStack.Count > 0;
    }

    private sealed record UndoOperation(string Description, Action Apply);
}
