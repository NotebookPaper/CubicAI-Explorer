using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using CubicAIExplorer.Models;
using CubicAIExplorer.ViewModels;

namespace CubicAIExplorer;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private GridViewColumnHeader? _lastHeaderClicked;
    private ListSortDirection _lastDirection = ListSortDirection.Ascending;
    private MainViewModel? _boundViewModel;
    private FileListViewModel? _boundFileListViewModel;
    private FileSystemItem? _inlineRenameItem;
    private Point _dragStartPoint;

    private const string InternalDragFormat = "CubicAIExplorer_InternalDrag";

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += MainWindow_DataContextChanged;
    }

    private void AddressBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ViewModel.NavigateToAddressCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem item && item.DataContext is FolderTreeNodeViewModel node)
        {
            ViewModel.SelectTreeNode(node);
            e.Handled = true;
        }
    }

    private void BookmarkList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.SelectedBookmark is { } bookmark)
        {
            ViewModel.NavigateBookmarkCommand.Execute(bookmark);
        }
    }

    private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.ActiveTab?.FileList.SelectedItem is { } item)
        {
            ViewModel.ActiveTab.FileList.OpenItemCommand.Execute(item);
        }
    }

    private void FileList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(FileListView);
    }

    private void FileList_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        var delta = e.GetPosition(FileListView) - _dragStartPoint;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var fileList = ViewModel.ActiveTab?.FileList;
        if (fileList == null) return;

        var paths = fileList.GetSelectedPathsForTransfer();
        if (paths.Count == 0) return;

        var dropList = new System.Collections.Specialized.StringCollection();
        dropList.AddRange(paths.ToArray());

        var data = new DataObject();
        data.SetFileDropList(dropList);
        data.SetData(InternalDragFormat, true);

        DragDrop.DoDragDrop(FileListView, data, DragDropEffects.Copy | DragDropEffects.Move);
    }

    private void FileList_DragEnter(object sender, DragEventArgs e)
    {
        UpdateDragEffects(e);
    }

    private void FileList_DragOver(object sender, DragEventArgs e)
    {
        UpdateDragEffects(e);
        e.Handled = true;
    }

    private void FileList_Drop(object sender, DragEventArgs e)
    {
        var fileList = ViewModel.ActiveTab?.FileList;
        if (fileList == null || !e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var droppedPaths = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (droppedPaths == null || droppedPaths.Length == 0)
            return;

        var destination = ResolveDropDestination(e.OriginalSource as DependencyObject) ?? fileList.CurrentPath;
        var moveFiles = ShouldMove(e);

        fileList.ImportDroppedFiles(droppedPaths, destination, moveFiles);
        e.Handled = true;
    }

    private void FileListHeader_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not GridViewColumnHeader header) return;
        if (header.Role == GridViewColumnHeaderRole.Padding) return;

        var direction = header != _lastHeaderClicked
            ? ListSortDirection.Ascending
            : _lastDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;

        var sortBy = header.Content?.ToString() switch
        {
            "Name" => nameof(FileSystemItem.Name),
            "Size" => nameof(FileSystemItem.Size),
            "Type" => nameof(FileSystemItem.TypeDescription),
            "Date Modified" => nameof(FileSystemItem.DateModified),
            _ => null
        };

        if (sortBy != null && sender is ListView listView)
        {
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(listView.ItemsSource);
            if (view != null)
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription(sortBy, direction));
            }
        }

        _lastHeaderClicked = header;
        _lastDirection = direction;
    }

    private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var fileListViewModel = ViewModel.ActiveTab?.FileList;
        if (fileListViewModel == null) return;

        fileListViewModel.SelectedItems.Clear();
        foreach (var selected in FileListView.SelectedItems.OfType<FileSystemItem>())
        {
            fileListViewModel.SelectedItems.Add(selected);
        }
    }

    private void FileList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        var fileListViewModel = ViewModel.ActiveTab?.FileList;
        if (fileListViewModel == null) return;

        var originalSource = e.OriginalSource as DependencyObject;
        var clickedItem = FindVisualParent<ListViewItem>(originalSource);
        var hasSelection = clickedItem != null
            && (fileListViewModel.SelectedItems.Count > 0 || fileListViewModel.SelectedItem != null);

        var itemVisibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
        var emptyVisibility = hasSelection ? Visibility.Collapsed : Visibility.Visible;

        OpenMenuItem.Visibility = itemVisibility;
        ItemSeparator1.Visibility = itemVisibility;
        CutMenuItem.Visibility = itemVisibility;
        CopyMenuItem.Visibility = itemVisibility;
        ItemSeparator2.Visibility = itemVisibility;
        DeleteMenuItem.Visibility = itemVisibility;
        RenameMenuItem.Visibility = itemVisibility;

        NewFolderMenuItem.Visibility = emptyVisibility;
        RefreshMenuItem.Visibility = emptyVisibility;
        PasteMenuItem.Visibility = Visibility.Visible;
    }

    private void OnExitClick(object sender, RoutedEventArgs e) => Close();

    private void OnAboutClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "CubicAI Explorer v0.1.0\n\n" +
            "A modern Windows file manager\n" +
            "inspired by CubicExplorer.\n\n" +
            "Built with the assistance of Claude AI.",
            "About CubicAI Explorer",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void MainWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_boundViewModel != null)
        {
            _boundViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        _boundViewModel = e.NewValue as MainViewModel;
        if (_boundViewModel != null)
        {
            _boundViewModel.PropertyChanged += ViewModel_PropertyChanged;
            HookFileListViewModel(_boundViewModel.ActiveTab?.FileList);
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.ActiveTab))
        {
            HookFileListViewModel(_boundViewModel?.ActiveTab?.FileList);
        }
    }

    private void HookFileListViewModel(FileListViewModel? fileListViewModel)
    {
        if (_boundFileListViewModel != null)
        {
            _boundFileListViewModel.SelectAllRequested -= FileListViewModel_SelectAllRequested;
            _boundFileListViewModel.InlineRenameRequested -= FileListViewModel_InlineRenameRequested;
        }

        _boundFileListViewModel = fileListViewModel;
        if (_boundFileListViewModel != null)
        {
            _boundFileListViewModel.SelectAllRequested += FileListViewModel_SelectAllRequested;
            _boundFileListViewModel.InlineRenameRequested += FileListViewModel_InlineRenameRequested;
        }
    }

    private void FileListViewModel_SelectAllRequested(object? sender, EventArgs e)
    {
        FileListView.SelectAll();
    }

    private void FileListViewModel_InlineRenameRequested(object? sender, FileSystemItem item)
    {
        BeginInlineRename(item);
    }

    private void BeginInlineRename(FileSystemItem item)
    {
        if (item == null) return;

        FileListView.ScrollIntoView(item);
        FileListView.UpdateLayout();

        var container = FileListView.ItemContainerGenerator.ContainerFromItem(item) as ListViewItem;
        if (container == null) return;

        var nameTextBlock = FindVisualChildren<TextBlock>(container)
            .FirstOrDefault(tb => tb.Name == "FileNameTextBlock");
        if (nameTextBlock == null) return;

        var point = nameTextBlock.TranslatePoint(new Point(0, 0), FileListView);
        InlineRenamePopup.HorizontalOffset = Math.Max(0, point.X - 2);
        InlineRenamePopup.VerticalOffset = Math.Max(0, point.Y - 1);
        InlineRenameTextBox.Width = Math.Max(180, nameTextBlock.ActualWidth + 18);
        InlineRenameTextBox.Text = item.Name;
        InlineRenamePopup.IsOpen = true;
        _inlineRenameItem = item;

        InlineRenameTextBox.Focus();
        SelectRenameText(item.Name);
    }

    private void InlineRenameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitInlineRename();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            CancelInlineRename();
            e.Handled = true;
        }
    }

    private void InlineRenameTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (InlineRenamePopup.IsOpen)
            CommitInlineRename();
    }

    private void CommitInlineRename()
    {
        var item = _inlineRenameItem;
        var fileList = _boundViewModel?.ActiveTab?.FileList;
        if (item != null && fileList != null)
        {
            var newName = InlineRenameTextBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(newName) && !string.Equals(newName, item.Name, StringComparison.Ordinal))
            {
                fileList.RenameItem(item, newName);
            }
        }

        CloseInlineRename();
    }

    private void CancelInlineRename()
    {
        CloseInlineRename();
    }

    private void CloseInlineRename()
    {
        InlineRenamePopup.IsOpen = false;
        _inlineRenameItem = null;
    }

    private void SelectRenameText(string fileName)
    {
        var dotIndex = fileName.LastIndexOf('.');
        var isDotFile = dotIndex == 0;
        var hasUsableExtension = dotIndex > 0 && dotIndex < fileName.Length - 1;

        if (hasUsableExtension && !isDotFile)
        {
            InlineRenameTextBox.Select(0, dotIndex);
            return;
        }

        InlineRenameTextBox.SelectAll();
    }

    private void UpdateDragEffects(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.None;
            return;
        }

        e.Effects = ShouldMove(e) ? DragDropEffects.Move : DragDropEffects.Copy;
    }

    private bool ShouldMove(DragEventArgs e)
    {
        var isInternalDrag = e.Data.GetDataPresent(InternalDragFormat);
        var forceCopy = (e.KeyStates & DragDropKeyStates.ControlKey) != 0;
        var forceMove = (e.KeyStates & DragDropKeyStates.ShiftKey) != 0;

        if (forceCopy) return false;
        if (forceMove) return true;
        return isInternalDrag;
    }

    private string? ResolveDropDestination(DependencyObject? source)
    {
        var itemContainer = FindVisualParent<ListViewItem>(source);
        if (itemContainer?.DataContext is not FileSystemItem targetItem)
            return null;

        return targetItem.ItemType switch
        {
            FileSystemItemType.Directory => targetItem.FullPath,
            FileSystemItemType.Drive => targetItem.FullPath,
            _ => null
        };
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T match)
                return match;

            child = System.Windows.Media.VisualTreeHelper.GetParent(child);
        }

        return null;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        var childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childCount; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
                yield return typedChild;

            foreach (var descendant in FindVisualChildren<T>(child))
                yield return descendant;
        }
    }
}
