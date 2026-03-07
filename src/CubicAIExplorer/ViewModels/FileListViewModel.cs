using System.Collections.ObjectModel;
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

    public ObservableCollection<FileSystemItem> Items { get; } = [];
    public ObservableCollection<FileSystemItem> SelectedItems { get; } = [];

    public event EventHandler<string>? NavigateRequested;
    public event EventHandler? SelectAllRequested;

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

        try
        {
            if (isCut)
            {
                _fileSystemService.MoveFiles(paths, CurrentPath);
                _clipboardService.Clear();
            }
            else
            {
                _fileSystemService.CopyFiles(paths, CurrentPath);
            }

            Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Paste failed: {ex.Message}",
                "Paste Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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

        var dialog = new RenameDialog(item.Name);
        if (dialog.ShowDialog() != true) return;

        try
        {
            _fileSystemService.RenameFile(item.FullPath, dialog.EnteredName);
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

    [RelayCommand]
    private void NewFolder()
    {
        if (string.IsNullOrWhiteSpace(CurrentPath)) return;

        var dialog = new NewFolderDialog();
        if (dialog.ShowDialog() != true) return;

        try
        {
            _fileSystemService.CreateFolder(CurrentPath, dialog.FolderName);
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
}
