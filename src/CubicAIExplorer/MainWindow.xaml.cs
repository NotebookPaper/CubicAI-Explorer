using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using CubicAIExplorer.Models;
using CubicAIExplorer.ViewModels;
using CubicAIExplorer.Views;

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
        PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl+F: focus filter bar
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            FilterTextBox.Focus();
            FilterTextBox.SelectAll();
            e.Handled = true;
            return;
        }

        // Ctrl+L: switch to address bar edit mode
        if (e.Key == Key.L && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SwitchToEditMode();
            e.Handled = true;
            return;
        }

        // Enter: open selected item (only when file list has focus)
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None
            && FileListView.IsKeyboardFocusWithin)
        {
            var item = ViewModel.ActiveTab?.FileList.SelectedItem;
            if (item != null)
            {
                ViewModel.ActiveTab!.FileList.OpenItemCommand.Execute(item);
                e.Handled = true;
            }
        }
    }

    private void AddressBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ViewModel.NavigateToAddressCommand.Execute(null);
            SwitchToBreadcrumbMode();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            SwitchToBreadcrumbMode();
            e.Handled = true;
        }
    }

    private void AddressBar_LostFocus(object sender, RoutedEventArgs e)
    {
        SwitchToBreadcrumbMode();
    }

    private void AddressBarGo_Click(object sender, RoutedEventArgs e)
    {
        SwitchToBreadcrumbMode();
    }

    private void BreadcrumbBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Click on empty area of breadcrumb bar to switch to edit mode
        if (e.OriginalSource is System.Windows.Controls.Border
            || e.OriginalSource is System.Windows.Controls.DockPanel
            || e.OriginalSource is System.Windows.Controls.Grid)
        {
            SwitchToEditMode();
        }
    }

    private void BreadcrumbSegment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string path)
        {
            ViewModel.NavigateToPath(path);
        }
    }

    private void SwitchToEditMode()
    {
        BreadcrumbBar.Visibility = Visibility.Collapsed;
        AddressBarPanel.Visibility = Visibility.Visible;
        AddressBar.Focus();
        AddressBar.SelectAll();
    }

    private void SwitchToBreadcrumbMode()
    {
        AddressBarPanel.Visibility = Visibility.Collapsed;
        BreadcrumbBar.Visibility = Visibility.Visible;
    }

    private void RecentFolders_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.ListBox lb
            && lb.SelectedItem is Models.RecentFolderItem recent)
        {
            ViewModel.NavigateToPath(recent.FullPath);
            lb.SelectedItem = null; // Reset selection so clicking the same item again works
        }
    }

    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ViewModel.ActiveTab?.FileList.ExecuteSearchCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            ViewModel.ActiveTab?.FileList.CloseSearchCommand.Execute(null);
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

    private void BookmarkList_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        // Strip any existing sort arrow from header text
        var headerText = header.Content?.ToString()?.TrimEnd(" \u25B2\u25BC".ToCharArray()) ?? "";

        var sortBy = headerText switch
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

            // Update sort arrows on all headers
            if (FileListView.View is GridView gridView)
            {
                foreach (var col in gridView.Columns)
                {
                    if (col.Header is string colHeader)
                        col.Header = colHeader.TrimEnd(" \u25B2\u25BC".ToCharArray());
                }
            }

            var arrow = direction == ListSortDirection.Ascending ? " \u25B2" : " \u25BC";
            header.Content = headerText + arrow;
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

        fileListViewModel.UpdateSelectionStatus();
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
        PropertiesSeparator.Visibility = Visibility.Visible;
        PropertiesMenuItem.Visibility = itemVisibility;
        OpenInExplorerMenuItem.Visibility = Visibility.Visible;
    }

    private void ViewMode_Details_Click(object sender, RoutedEventArgs e) => SetViewMode("Details");
    private void ViewMode_List_Click(object sender, RoutedEventArgs e) => SetViewMode("List");
    private void ViewMode_Tiles_Click(object sender, RoutedEventArgs e) => SetViewMode("Tiles");

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        var fileList = ViewModel.ActiveTab?.FileList;
        if (fileList != null)
            fileList.FilterText = string.Empty;
    }

    private void SetViewMode(string mode)
    {
        var fileList = ViewModel.ActiveTab?.FileList;
        if (fileList != null)
            fileList.ViewMode = mode;
    }

    private void FolderTree_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var treeItem = FindVisualParent<TreeViewItem>(e.OriginalSource as DependencyObject);
        if (treeItem?.DataContext is FolderTreeNodeViewModel)
        {
            e.Effects = (e.KeyStates & DragDropKeyStates.ControlKey) != 0
                ? DragDropEffects.Copy
                : DragDropEffects.Move;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void FolderTree_Drop(object sender, DragEventArgs e)
    {
        var fileList = ViewModel.ActiveTab?.FileList;
        if (fileList == null || !e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var treeItem = FindVisualParent<TreeViewItem>(e.OriginalSource as DependencyObject);
        if (treeItem?.DataContext is not FolderTreeNodeViewModel targetNode)
            return;

        var droppedPaths = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (droppedPaths == null || droppedPaths.Length == 0)
            return;

        var moveFiles = (e.KeyStates & DragDropKeyStates.ControlKey) == 0;
        fileList.ImportDroppedFiles(droppedPaths, targetNode.FullPath, moveFiles);
        e.Handled = true;
    }

    private void OpenInExplorer_Click(object sender, RoutedEventArgs e)
    {
        var path = ViewModel.ActiveTab?.CurrentPath;
        if (!string.IsNullOrWhiteSpace(path))
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
    }

    private void OnExitClick(object sender, RoutedEventArgs e) => Close();

    private void DuplicateTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: TabViewModel tab })
        {
            ViewModel.DuplicateTab(tab);
        }
    }

    private void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: TabViewModel tab })
        {
            ViewModel.CloseTabCommand.Execute(tab);
        }
    }

    private void CloseOtherTabs_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: TabViewModel tab })
        {
            ViewModel.CloseOtherTabs(tab);
        }
    }

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
            _boundFileListViewModel.ViewModeChanged -= FileListViewModel_ViewModeChanged;
            _boundFileListViewModel.PropertiesRequested -= FileListViewModel_PropertiesRequested;
            _boundFileListViewModel.SearchPanelOpened -= FileListViewModel_SearchPanelOpened;
        }

        _boundFileListViewModel = fileListViewModel;
        if (_boundFileListViewModel != null)
        {
            _boundFileListViewModel.SelectAllRequested += FileListViewModel_SelectAllRequested;
            _boundFileListViewModel.InlineRenameRequested += FileListViewModel_InlineRenameRequested;
            _boundFileListViewModel.ViewModeChanged += FileListViewModel_ViewModeChanged;
            _boundFileListViewModel.PropertiesRequested += FileListViewModel_PropertiesRequested;
            _boundFileListViewModel.SearchPanelOpened += FileListViewModel_SearchPanelOpened;
            // Only apply non-default view modes; Details is already set in XAML
            if (_boundFileListViewModel.ViewMode != "Details")
                ApplyViewMode(_boundFileListViewModel.ViewMode);
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

    private void FileListViewModel_ViewModeChanged(object? sender, string mode)
    {
        ApplyViewMode(mode);
    }

    private void FileListViewModel_PropertiesRequested(object? sender, FileSystemItem item)
    {
        var dialog = new PropertiesDialog(item) { Owner = this };
        dialog.ShowDialog();
    }

    private void FileListViewModel_SearchPanelOpened(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            SearchTextBox.Focus();
            SearchTextBox.SelectAll();
        }, System.Windows.Threading.DispatcherPriority.Input);
    }

    private void ApplyViewMode(string mode)
    {
        switch (mode)
        {
            case "Details":
                FileListView.View = CreateDetailsGridView();
                FileListView.ItemTemplate = null;
                FileListView.ItemsPanel = null;
                break;
            case "List":
                FileListView.View = null;
                FileListView.ItemTemplate = (DataTemplate)FindResource("ListViewItemTemplate");
                FileListView.ItemsPanel = null;
                break;
            case "Tiles":
                FileListView.View = null;
                FileListView.ItemTemplate = (DataTemplate)FindResource("TileViewItemTemplate");
                FileListView.ItemsPanel = (ItemsPanelTemplate)FindResource("TileItemsPanelTemplate");
                break;
        }
    }

    private GridView CreateDetailsGridView()
    {
        var gridView = new GridView();

        var nameColumn = new GridViewColumn { Header = "Name", Width = 350 };
        nameColumn.CellTemplate = (DataTemplate)FindResource("DetailsNameCellTemplate");
        gridView.Columns.Add(nameColumn);

        gridView.Columns.Add(new GridViewColumn
        {
            Header = "Size",
            Width = 100,
            DisplayMemberBinding = new System.Windows.Data.Binding("DisplaySize")
        });
        gridView.Columns.Add(new GridViewColumn
        {
            Header = "Type",
            Width = 120,
            DisplayMemberBinding = new System.Windows.Data.Binding("TypeDescription")
        });
        gridView.Columns.Add(new GridViewColumn
        {
            Header = "Date Modified",
            Width = 160,
            DisplayMemberBinding = new System.Windows.Data.Binding("DateModified")
            {
                StringFormat = "{0:yyyy-MM-dd HH:mm}"
            }
        });

        return gridView;
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
