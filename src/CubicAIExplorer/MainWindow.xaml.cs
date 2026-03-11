using System.IO;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using CubicAIExplorer.Models;
using CubicAIExplorer.Services;
using CubicAIExplorer.ViewModels;
using CubicAIExplorer.Views;

namespace CubicAIExplorer;

public partial class MainWindow : Window
{
    private static readonly DetailsColumnId[] DetailsColumnOrder =
    [
        DetailsColumnId.Name,
        DetailsColumnId.Size,
        DetailsColumnId.Type,
        DetailsColumnId.DateModified,
        DetailsColumnId.Company,
        DetailsColumnId.Version,
        DetailsColumnId.Dimensions,
        DetailsColumnId.Duration
    ];

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private GridViewColumnHeader? _lastHeaderClicked;
    private ListSortDirection _lastDirection = ListSortDirection.Ascending;
    private GridViewColumnHeader? _lastRightHeaderClicked;
    private ListSortDirection _lastRightDirection = ListSortDirection.Ascending;
    private MainViewModel? _boundViewModel;
    private FileListViewModel? _boundFileListViewModel;
    private FileListViewModel? _boundRightFileListViewModel;
    private FileSystemItem? _inlineRenameItem;
    private FileListViewModel? _inlineRenameFileListViewModel;
    private ListView? _inlineRenameListView;
    private ScrollViewer? _tabHeaderScrollViewer;
    private Button? _tabScrollLeftButton;
    private Button? _tabScrollRightButton;
    private Button? _tabOverflowButton;
    private ContextMenu? _tabOverflowMenu;
    private Point _dragStartPoint;
    private Point _rightPaneDragStartPoint;
    private bool _suppressAutoComplete;
    private bool _suppressRightPaneAutoComplete;
    private bool _isApplyingDetailsLayout;

    private const string InternalDragFormat = "CubicAIExplorer_InternalDrag";
    private static readonly Brush ActivePaneBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x66, 0xAA));
    private static readonly Brush InactivePaneBrush = new SolidColorBrush(Color.FromRgb(0x9F, 0xB7, 0xD6));
    private static readonly Brush ActiveHeaderGradient = CreateFrozenBrush(
        new LinearGradientBrush(Color.FromRgb(0xF0, 0xF6, 0xFF), Color.FromRgb(0xCF, 0xE0, 0xF8), 90));

    private static Brush CreateFrozenBrush(Brush brush)
    {
        brush.Freeze();
        return brush;
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += MainWindow_DataContextChanged;
        PreviewKeyDown += MainWindow_PreviewKeyDown;
        Closing += MainWindow_Closing;
        Loaded += MainWindow_Loaded;
        SizeChanged += MainWindow_SizeChanged;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        EnsureTabStripParts();
        QueueTabStripUpdate(scrollActiveIntoView: true);
        QueueFilePaneVisibilityCheck();
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        QueueTabStripUpdate(scrollActiveIntoView: false);
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl+1: focus folder tree
        if (e.Key == Key.D1 && Keyboard.Modifiers == ModifierKeys.Control)
        {
            FolderTree.Focus();
            e.Handled = true;
            return;
        }

        // Ctrl+2: focus left file pane
        if (e.Key == Key.D2 && Keyboard.Modifiers == ModifierKeys.Control)
        {
            ViewModel.ActivateLeftPane();
            FileListView.Focus();
            e.Handled = true;
            return;
        }

        // Ctrl+3: focus right file pane (or left pane when dual-pane is disabled)
        if (e.Key == Key.D3 && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (ViewModel.IsDualPaneMode && ViewModel.RightPaneTab != null)
            {
                ViewModel.ActivateRightPane();
                RightPaneListView.Focus();
            }
            else
            {
                ViewModel.ActivateLeftPane();
                FileListView.Focus();
            }
            e.Handled = true;
            return;
        }

        // Ctrl+4: focus preview panel
        if (e.Key == Key.D4 && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (!ViewModel.IsPreviewVisible)
                ViewModel.TogglePreviewCommand.Execute(null);

            Dispatcher.BeginInvoke(() => PreviewPanel.Focus(),
                System.Windows.Threading.DispatcherPriority.Input);
            e.Handled = true;
            return;
        }

        // Ctrl+F: focus filter bar
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            FilterTextBox.Focus();
            FilterTextBox.SelectAll();
            e.Handled = true;
            return;
        }

        // Ctrl+L / Alt+D: switch to address bar edit mode
        if ((e.Key == Key.L && Keyboard.Modifiers == ModifierKeys.Control)
            || (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Alt))
        {
            SwitchToEditMode();
            e.Handled = true;
            return;
        }

        // Ctrl+Shift+L: switch to right-pane address edit mode
        if (e.Key == Key.L
            && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)
            && ViewModel.IsDualPaneMode && ViewModel.RightPaneTab != null)
        {
            SwitchToRightPaneAddressEditMode();
            e.Handled = true;
            return;
        }

        // Enter: open selected item (only when file list has focus)
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None
            && (FileListView.IsKeyboardFocusWithin || RightPaneListView.IsKeyboardFocusWithin))
        {
            var item = ViewModel.CurrentPaneFileList?.SelectedItem;
            if (item != null)
            {
                ViewModel.CurrentPaneFileList!.OpenItemCommand.Execute(item);
                e.Handled = true;
            }
        }
    }

    private void AddressBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (AddressAutoCompletePopup.IsOpen && AutoCompleteList.SelectedItem is string selected)
            {
                AcceptAutoCompleteSuggestion(selected);
            }
            else
            {
                AddressAutoCompletePopup.IsOpen = false;
                ViewModel.NavigateToAddressCommand.Execute(null);
                SwitchToBreadcrumbMode();
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            if (AddressAutoCompletePopup.IsOpen)
            {
                AddressAutoCompletePopup.IsOpen = false;
            }
            else
            {
                SwitchToBreadcrumbMode();
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Down && AddressAutoCompletePopup.IsOpen)
        {
            AutoCompleteList.SelectedIndex = Math.Min(
                AutoCompleteList.SelectedIndex + 1,
                AutoCompleteList.Items.Count - 1);
            e.Handled = true;
        }
        else if (e.Key == Key.Up && AddressAutoCompletePopup.IsOpen)
        {
            AutoCompleteList.SelectedIndex = Math.Max(AutoCompleteList.SelectedIndex - 1, 0);
            e.Handled = true;
        }
        else if (e.Key == Key.Tab && AddressAutoCompletePopup.IsOpen
            && AutoCompleteList.SelectedItem is string tabSelected)
        {
            // Tab fills the path and continues drilling (appends \ and shows subdirectories)
            var path = tabSelected.TrimEnd('\\') + "\\";
            _suppressAutoComplete = true;
            ViewModel.AddressBarText = path;
            AddressBar.CaretIndex = path.Length;
            _suppressAutoComplete = false;
            ViewModel.UpdateAddressSuggestions();
            AddressAutoCompletePopup.IsOpen = ViewModel.IsAddressSuggestionsOpen;
            e.Handled = true;
        }
    }

    private void AddressBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressAutoComplete) return;
        ViewModel.UpdateAddressSuggestions();
        AddressAutoCompletePopup.IsOpen = ViewModel.IsAddressSuggestionsOpen;
        if (AddressAutoCompletePopup.IsOpen && AutoCompleteList.Items.Count > 0)
            AutoCompleteList.SelectedIndex = 0;
    }

    private void AutoComplete_Select(object sender, MouseButtonEventArgs e)
    {
        if (AutoCompleteList.SelectedItem is string path)
        {
            AcceptAutoCompleteSuggestion(path);
        }
    }

    private void AcceptAutoCompleteSuggestion(string path)
    {
        _suppressAutoComplete = true;
        ViewModel.AddressBarText = path;
        AddressBar.CaretIndex = path.Length;
        _suppressAutoComplete = false;
        AddressAutoCompletePopup.IsOpen = false;

        ViewModel.NavigateCurrentPaneToPath(path);
        SwitchToBreadcrumbMode();
    }

    private void AddressBar_LostFocus(object sender, RoutedEventArgs e)
    {
        if (AddressAutoCompletePopup.IsMouseOver) return;
        AddressAutoCompletePopup.IsOpen = false;
        SwitchToBreadcrumbMode();
    }

    private void AddressBarGo_Click(object sender, RoutedEventArgs e)
    {
        SwitchToBreadcrumbMode();
    }

    private void BreadcrumbBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        // Any click in the breadcrumb chrome that is not on an actual button should
        // switch to editable path mode so the full path can be copied/pasted quickly.
        if (FindVisualParent<System.Windows.Controls.Button>(e.OriginalSource as DependencyObject) != null)
            return;

        SwitchToEditMode();
        e.Handled = true;
    }

    private void BreadcrumbSegment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string path)
        {
            ViewModel.NavigateCurrentPaneToPath(path);
        }
    }

    private async void BreadcrumbDropdownButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button
            || button.DataContext is not BreadcrumbSegment segment
            || button.ContextMenu is not ContextMenu menu)
        {
            return;
        }

        menu.PlacementTarget = button;
        menu.IsOpen = true;
        await ViewModel.LoadBreadcrumbDropdownAsync(segment);
        e.Handled = true;
    }

    private void BreadcrumbDropdownItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem menuItem
            && menuItem.DataContext is BreadcrumbDropdownItem item)
        {
            ViewModel.NavigateBreadcrumbDropdownItem(item);
            e.Handled = true;
        }
    }

    private void SwitchToEditMode_Click(object sender, RoutedEventArgs e)
    {
        SwitchToEditMode();
    }

    private void AddressBar_GotFocus(object sender, RoutedEventArgs e)
    {
        AddressBar.SelectAll();
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

    private void SwitchToRightPaneAddressEditMode()
    {
        ViewModel.ActivateRightPane();
        RightPaneAddressDisplay.Visibility = Visibility.Collapsed;
        RightPaneStatusBlock.Visibility = Visibility.Collapsed;
        RightPaneAddressEditorPanel.Visibility = Visibility.Visible;
        RightPaneAddressBox.Focus();
        RightPaneAddressBox.SelectAll();
    }

    private void ExitRightPaneAddressEditMode()
    {
        RightPaneAutoCompletePopup.IsOpen = false;
        RightPaneAddressEditorPanel.Visibility = Visibility.Collapsed;
        RightPaneAddressDisplay.Visibility = Visibility.Visible;
        RightPaneStatusBlock.Visibility = Visibility.Visible;
    }

    private void CommitRightPaneAddressEdit()
    {
        RightPaneAutoCompletePopup.IsOpen = false;
        ViewModel.ActivateRightPane();
        ViewModel.NavigateCurrentPaneToPath(ViewModel.RightPaneAddressText);
        ExitRightPaneAddressEditMode();
        RightPaneListView.Focus();
    }

    private void CancelRightPaneAddressEdit()
    {
        ViewModel.RightPaneAddressText = ViewModel.RightPaneTab?.CurrentPath ?? string.Empty;
        ExitRightPaneAddressEditMode();
    }

    private void RecentFolders_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is System.Windows.Controls.ListBox lb
            && lb.SelectedItem is Models.RecentFolderItem recent)
        {
            ViewModel.NavigateCurrentPaneToPath(recent.FullPath);
            SwitchToBreadcrumbMode();
            lb.SelectedItem = null; // Reset selection so clicking the same item again works
        }
    }

    private void RecentFolders_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (sender is not ListBox { SelectedItem: RecentFolderItem recent }) return;

        ViewModel.NavigateCurrentPaneToPath(recent.FullPath);
        e.Handled = true;
    }

    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ViewModel.ExecuteSearchCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            ViewModel.CloseSearchCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void RightPaneHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (RightPaneAddressEditorPanel.Visibility == Visibility.Visible)
            return;

        ViewModel.ActivateRightPane();
        if (e.ClickCount >= 2)
        {
            SwitchToRightPaneAddressEditMode();
        }
        else
        {
            RightPaneListView.Focus();
        }
        e.Handled = true;
    }

    private void RightPaneAddressBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (RightPaneAutoCompletePopup.IsOpen && RightPaneAutoCompleteList.SelectedItem is string selected)
            {
                AcceptRightPaneAutoCompleteSuggestion(selected);
            }
            else
            {
                CommitRightPaneAddressEdit();
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            if (RightPaneAutoCompletePopup.IsOpen)
            {
                RightPaneAutoCompletePopup.IsOpen = false;
            }
            else
            {
                CancelRightPaneAddressEdit();
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Down && RightPaneAutoCompletePopup.IsOpen)
        {
            RightPaneAutoCompleteList.SelectedIndex = Math.Min(
                RightPaneAutoCompleteList.SelectedIndex + 1,
                RightPaneAutoCompleteList.Items.Count - 1);
            e.Handled = true;
        }
        else if (e.Key == Key.Up && RightPaneAutoCompletePopup.IsOpen)
        {
            RightPaneAutoCompleteList.SelectedIndex = Math.Max(
                RightPaneAutoCompleteList.SelectedIndex - 1, 0);
            e.Handled = true;
        }
        else if (e.Key == Key.Tab && RightPaneAutoCompletePopup.IsOpen
            && RightPaneAutoCompleteList.SelectedItem is string tabSelected)
        {
            // Tab fills the path and continues drilling
            var path = tabSelected.TrimEnd('\\') + "\\";
            _suppressRightPaneAutoComplete = true;
            ViewModel.RightPaneAddressText = path;
            RightPaneAddressBox.CaretIndex = path.Length;
            _suppressRightPaneAutoComplete = false;
            ViewModel.UpdateRightPaneAddressSuggestions();
            RightPaneAutoCompletePopup.IsOpen = ViewModel.IsRightPaneSuggestionsOpen;
            e.Handled = true;
        }
    }

    private void RightPaneAddressBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressRightPaneAutoComplete) return;
        ViewModel.UpdateRightPaneAddressSuggestions();
        RightPaneAutoCompletePopup.IsOpen = ViewModel.IsRightPaneSuggestionsOpen;
        if (RightPaneAutoCompletePopup.IsOpen && RightPaneAutoCompleteList.Items.Count > 0)
            RightPaneAutoCompleteList.SelectedIndex = 0;
    }

    private void RightPaneAutoComplete_Select(object sender, MouseButtonEventArgs e)
    {
        if (RightPaneAutoCompleteList.SelectedItem is string path)
        {
            AcceptRightPaneAutoCompleteSuggestion(path);
        }
    }

    private void AcceptRightPaneAutoCompleteSuggestion(string path)
    {
        _suppressRightPaneAutoComplete = true;
        ViewModel.RightPaneAddressText = path;
        RightPaneAddressBox.CaretIndex = path.Length;
        _suppressRightPaneAutoComplete = false;
        RightPaneAutoCompletePopup.IsOpen = false;

        ViewModel.ActivateRightPane();
        ViewModel.NavigateCurrentPaneToPath(path);
        ExitRightPaneAddressEditMode();
        RightPaneListView.Focus();
    }

    private void RightPaneAddressBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (RightPaneAddressEditorPanel.IsKeyboardFocusWithin
            || RightPaneAutoCompletePopup.IsMouseOver)
            return;

        CancelRightPaneAddressEdit();
    }

    private void RightPaneAddressGo_Click(object sender, RoutedEventArgs e)
    {
        CommitRightPaneAddressEdit();
    }

    private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem item && item.DataContext is FolderTreeNodeViewModel node)
        {
            ViewModel.SelectTreeNode(node);
            e.Handled = true;
        }
    }

    private void BookmarkItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is TreeViewItem item && item.DataContext is BookmarkItem bookmark)
        {
            if (!bookmark.IsFolder || !string.IsNullOrWhiteSpace(bookmark.Path))
            {
                ViewModel.NavigateBookmarkCommand.Execute(bookmark);
                e.Handled = true;
            }
        }
    }

    private void BookmarkItem_Selected(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem item && item.DataContext is BookmarkItem bookmark)
        {
            ViewModel.SelectedBookmark = bookmark;
            if (!string.IsNullOrWhiteSpace(bookmark.Path))
            {
                ViewModel.NavigateBookmarkCommand.Execute(bookmark);
            }
            e.Handled = true;
        }
    }

    private void BookmarkBarButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: BookmarkItem bookmark })
        {
            ViewModel.NavigateBookmarkCommand.Execute(bookmark);
            e.Handled = true;
        }
    }

    private void BookmarkBarOpen_Click(object sender, RoutedEventArgs e)
    {
        if (TryGetBookmarkBarContextBookmark(sender, out var bookmark))
        {
            ViewModel.NavigateBookmarkCommand.Execute(bookmark);
            e.Handled = true;
        }
    }

    private void BookmarkBarOpenInNewTab_Click(object sender, RoutedEventArgs e)
    {
        if (TryGetBookmarkBarContextBookmark(sender, out var bookmark))
        {
            ViewModel.OpenBookmarkInNewTabCommand.Execute(bookmark);
            e.Handled = true;
        }
    }

    private void BookmarkBarRename_Click(object sender, RoutedEventArgs e)
    {
        if (TryGetBookmarkBarContextBookmark(sender, out var bookmark))
        {
            ViewModel.RenameBookmarkCommand.Execute(bookmark);
            e.Handled = true;
        }
    }

    private void BookmarkBarDelete_Click(object sender, RoutedEventArgs e)
    {
        if (TryGetBookmarkBarContextBookmark(sender, out var bookmark))
        {
            ViewModel.RemoveBookmarkCommand.Execute(bookmark);
            e.Handled = true;
        }
    }

    private static bool TryGetBookmarkBarContextBookmark(object sender, out BookmarkItem? bookmark)
    {
        bookmark = (sender as FrameworkElement)?.DataContext as BookmarkItem;
        if (bookmark != null)
            return true;

        if (sender is MenuItem { Parent: ContextMenu { PlacementTarget: FrameworkElement target } })
        {
            bookmark = target.DataContext as BookmarkItem;
            return bookmark != null;
        }

        return false;
    }

    private void BookmarkTree_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _bookmarkDragStart = e.GetPosition(BookmarkTree);
    }

    private Point _bookmarkDragStart;
    private const string BookmarkDragFormat = "CubicAIExplorer_BookmarkDrag";

    private void BookmarkTree_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var delta = e.GetPosition(BookmarkTree) - _bookmarkDragStart;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        if (BookmarkTree.SelectedItem is BookmarkItem bookmark)
        {
            var data = new DataObject();
            data.SetData(BookmarkDragFormat, bookmark);
            try
            {
                DragDrop.DoDragDrop(BookmarkTree, data, DragDropEffects.Move);
            }
            finally
            {
                ViewModel.ClearBookmarkDragFeedback();
            }
        }
    }

    private void BookmarkTree_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(BookmarkDragFormat))
        {
            ViewModel.ClearBookmarkDragFeedback();
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var source = e.Data.GetData(BookmarkDragFormat) as BookmarkItem;
        var targetItem = FindVisualParent<TreeViewItem>(e.OriginalSource as DependencyObject);
        var target = targetItem?.DataContext as BookmarkItem;

        ViewModel.UpdateBookmarkDragFeedback(source, target);
        e.Effects = ViewModel.CanDropBookmark(source, target)
            ? DragDropEffects.Move
            : DragDropEffects.None;

        e.Handled = true;
    }

    private void BookmarkTree_DragLeave(object sender, DragEventArgs e)
    {
        ViewModel.ClearBookmarkDragFeedback();
        e.Handled = true;
    }

    private void BookmarkTree_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(BookmarkDragFormat)) return;

        var source = e.Data.GetData(BookmarkDragFormat) as BookmarkItem;
        var targetItem = FindVisualParent<TreeViewItem>(e.OriginalSource as DependencyObject);
        var target = targetItem?.DataContext as BookmarkItem;

        if (ViewModel.CanDropBookmark(source, target))
        {
            ViewModel.MoveBookmark(source!, target);
        }

        ViewModel.ClearBookmarkDragFeedback();
        e.Handled = true;
    }

    private void ImportBookmarks_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Import Bookmarks",
            Filter = "Cubic Explorer Bookmarks (*.xml)|*.xml|JSON Bookmarks (*.json)|*.json|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            ViewModel.ImportBookmarksCommand.Execute(dialog.FileName);
        }
    }

    private void ExportBookmarks_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Bookmarks",
            Filter = "JSON Bookmarks (*.json)|*.json|All Files (*.*)|*.*",
            FileName = "bookmarks.json"
        };

        if (dialog.ShowDialog(this) == true)
        {
            ViewModel.ExportBookmarksCommand.Execute(dialog.FileName);
        }
    }

    private void SavedSearchList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.SelectedSavedSearch is { } savedSearch)
            ViewModel.RunSavedSearchCommand.Execute(savedSearch);
    }

    private void SavedSearchList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Keep selection for context actions; execution is explicit via Enter, double-click, or the context menu.
    }

    private void SavedSearchList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ViewModel.SelectedSavedSearch is { } savedSearch)
        {
            ViewModel.RunSavedSearchCommand.Execute(savedSearch);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Delete && ViewModel.SelectedSavedSearch is { } selected)
        {
            ViewModel.RemoveSavedSearchCommand.Execute(selected);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F2 && ViewModel.SelectedSavedSearch is { } renameTarget)
        {
            ViewModel.RenameSavedSearchCommand.Execute(renameTarget);
            e.Handled = true;
        }
    }

    private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ViewModel.ActivateLeftPane();
        OpenSelectedInPane(ViewModel.ActiveTab?.FileList);
    }

    private void FileList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        ViewModel.ActivateLeftPane();
        _dragStartPoint = e.GetPosition(FileListView);
    }

    private void FileList_MouseMove(object sender, MouseEventArgs e)
    {
        TryInitiateDrag(e, FileListView, _dragStartPoint, ViewModel.ActiveTab?.FileList);
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

    private async void FileList_Drop(object sender, DragEventArgs e)
    {
        await HandleDropAsync(e, ViewModel.ActiveTab?.FileList);
    }

    private void FileListHeader_Click(object sender, RoutedEventArgs e)
    {
        SortListViewColumn(FileListView, e, ref _lastHeaderClicked, ref _lastDirection);
    }

    private void RightPaneHeader_Click(object sender, RoutedEventArgs e)
    {
        SortListViewColumn(RightPaneListView, e, ref _lastRightHeaderClicked, ref _lastRightDirection);
    }

    private void SortListViewColumn(
        ListView listView,
        RoutedEventArgs e,
        ref GridViewColumnHeader? lastHeaderClicked,
        ref ListSortDirection lastDirection)
    {
        if (e.OriginalSource is not GridViewColumnHeader header) return;
        if (header.Role == GridViewColumnHeaderRole.Padding) return;

        var direction = header != lastHeaderClicked
            ? ListSortDirection.Ascending
            : lastDirection == ListSortDirection.Ascending
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
            "Company" => "ShellProperties.Company",
            "Version" => "ShellProperties.FileVersion",
            "Dimensions" => "ShellProperties.Dimensions",
            "Duration" => "ShellProperties.Duration",
            _ => null
        };

        if (sortBy != null)
        {
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(listView.ItemsSource);
            if (view != null)
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription(sortBy, direction));
            }

            // Update sort arrows on all headers
            if (listView.View is GridView gridView)
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

        lastHeaderClicked = header;
        lastDirection = direction;
    }

    private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.ActivateLeftPane();
        SyncSelection(FileListView, ViewModel.ActiveTab?.FileList);
    }

    private void FileList_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        ViewModel.ActivateLeftPane();
    }

    private void FolderTree_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (ViewModel.CurrentSettings.UseShellContextMenu)
        {
            if (FolderTree.SelectedItem is FolderTreeNodeViewModel node && !string.IsNullOrWhiteSpace(node.FullPath))
            {
                if (ShowShellContextMenuForPaths(FolderTree, new List<string> { node.FullPath }))
                {
                    e.Handled = true;
                    return;
                }
            }
        }
    }

    private void BookmarkTree_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (ViewModel.CurrentSettings.UseShellContextMenu)
        {
            if (BookmarkTree.SelectedItem is BookmarkItem item && !string.IsNullOrWhiteSpace(item.Path))
            {
                // Only use shell menu if it's a real filesystem path
                if (Path.IsPathRooted(item.Path) && (Directory.Exists(item.Path) || File.Exists(item.Path)))
                {
                    if (ShowShellContextMenuForPaths(BookmarkTree, new List<string> { item.Path }))
                    {
                        e.Handled = true;
                        return;
                    }
                }
            }
        }
    }

    private void FileList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        ViewModel.ActivateLeftPane();

        if (ViewModel.CurrentSettings.UseShellContextMenu)
        {
            if (ShowShellContextMenu(FileListView, ViewModel.ActiveTab?.FileList, e))
            {
                e.Handled = true;
                return;
            }
        }

        ConfigureContextMenu(e, ViewModel.ActiveTab?.FileList,
            OpenMenuItem, BrowseArchiveMenuItem, ItemSeparator1, CutMenuItem, CopyMenuItem, ItemSeparator2,
            DeleteMenuItem, RenameMenuItem, NewMenuItem, RefreshMenuItem,
            PasteMenuItem, ExtractArchiveMenuItem, PropertiesSeparator, PropertiesMenuItem, OpenInExplorerMenuItem);
    }

    private void RightPane_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        ViewModel.ActivateRightPane();

        if (ViewModel.CurrentSettings.UseShellContextMenu)
        {
            if (ShowShellContextMenu(RightPaneListView, ViewModel.RightPaneTab?.FileList, e))
            {
                e.Handled = true;
                return;
            }
        }

        ConfigureContextMenu(e, ViewModel.RightPaneTab?.FileList,
            RightOpenMenuItem, RightBrowseArchiveMenuItem, RightItemSeparator1, RightCutMenuItem, RightCopyMenuItem, RightItemSeparator2,
            RightDeleteMenuItem, RightRenameMenuItem, RightNewMenuItem, RightRefreshMenuItem,
            RightPasteMenuItem, RightExtractArchiveMenuItem, RightPropertiesSeparator, RightPropertiesMenuItem, RightOpenInExplorerMenuItem);
    }

    private bool ShowShellContextMenu(ListView listView, FileListViewModel? fileList, ContextMenuEventArgs e)
    {
        if (fileList == null) return false;

        // Determine if an item was clicked or if it's background
        bool isBackground = true;
        if (e.OriginalSource is DependencyObject dep)
        {
            var item = FindVisualParent<ListViewItem>(dep);
            if (item != null)
            {
                isBackground = false;
            }
        }

        if (isBackground)
        {
            if (string.IsNullOrWhiteSpace(fileList.CurrentPath)) return false;

            try
            {
                var cursor = GetCursorPos();
                return ShellContextMenuHelper.ShowBackgroundContextMenu(
                    new WindowInteropHelper(this).Handle,
                    fileList.CurrentPath,
                    cursor.X,
                    cursor.Y);
            }
            catch
            {
                return false;
            }
        }

        var paths = fileList.SelectedItems.Count > 0
            ? fileList.SelectedItems.Select(static i => i.FullPath).ToList()
            : !string.IsNullOrWhiteSpace(fileList.CurrentPath)
                ? [fileList.CurrentPath]
                : new List<string>();

        return ShowShellContextMenuForPaths(listView, paths);
    }

    private bool ShowShellContextMenuForPaths(FrameworkElement element, List<string> paths)
    {
        if (paths.Count == 0) return false;

        try
        {
            var cursor = GetCursorPos();
            
            return ShellContextMenuHelper.ShowContextMenu(
                new WindowInteropHelper(this).Handle,
                paths,
                cursor.X,
                cursor.Y);
        }
        catch
        {
            return false;
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private static POINT GetCursorPos()
    {
        GetCursorPos(out var lpPoint);
        return lpPoint;
    }

    private void ConfigureContextMenu(
        ContextMenuEventArgs e, FileListViewModel? fileList,
        FrameworkElement open, FrameworkElement browseArchive, FrameworkElement sep1, FrameworkElement cut, FrameworkElement copy,
        FrameworkElement sep2, FrameworkElement delete, FrameworkElement rename,
        FrameworkElement newMenu, FrameworkElement refresh, FrameworkElement paste, FrameworkElement extractArchive,
        FrameworkElement propsSep, FrameworkElement props, FrameworkElement openInExplorer)
    {
        if (fileList == null) return;

        var originalSource = e.OriginalSource as DependencyObject;
        var clickedItem = FindVisualParent<ListViewItem>(originalSource);
        var hasSelection = clickedItem != null
            && (fileList.SelectedItems.Count > 0 || fileList.SelectedItem != null);

        var itemVisibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
        var emptyVisibility = hasSelection ? Visibility.Collapsed : Visibility.Visible;
        var selectedArchive = fileList.SelectedItems.Count == 1
            ? fileList.SelectedItems[0]
            : fileList.SelectedItems.Count == 0
                ? fileList.SelectedItem
                : null;
        var archiveVisibility = FileListViewModel.IsArchiveItem(selectedArchive)
            ? Visibility.Visible
            : Visibility.Collapsed;

        open.Visibility = itemVisibility;
        browseArchive.Visibility = archiveVisibility;
        sep1.Visibility = itemVisibility;
        cut.Visibility = itemVisibility;
        copy.Visibility = itemVisibility;
        sep2.Visibility = itemVisibility;
        delete.Visibility = itemVisibility;
        rename.Visibility = itemVisibility;

        newMenu.Visibility = emptyVisibility;
        refresh.Visibility = emptyVisibility;
        paste.Visibility = Visibility.Visible;
        extractArchive.Visibility = archiveVisibility;
        propsSep.Visibility = Visibility.Visible;
        props.Visibility = itemVisibility;
        openInExplorer.Visibility = Visibility.Visible;
    }

    private void ViewMode_Details_Click(object sender, RoutedEventArgs e) => SetViewMode("Details");
    private void ViewMode_List_Click(object sender, RoutedEventArgs e) => SetViewMode("List");
    private void ViewMode_Tiles_Click(object sender, RoutedEventArgs e) => SetViewMode("Tiles");

    private void EditNewMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
            PopulateNewMenu(menuItem, isMainMenu: true);
    }

    private void PaneNewMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
            PopulateNewMenu(menuItem, isMainMenu: false);
    }

    private void PopulateNewMenu(MenuItem menuItem, bool isMainMenu)
    {
        ViewModel.RefreshNewFileTemplatesCatalog();

        menuItem.Items.Clear();
        menuItem.Items.Add(new MenuItem
        {
            Header = "_Folder",
            InputGestureText = isMainMenu ? "Ctrl+N" : string.Empty,
            Command = ViewModel.NewFolderCommand
        });
        menuItem.Items.Add(new MenuItem
        {
            Header = "_File",
            Command = ViewModel.NewFileCommand
        });
        menuItem.Items.Add(new Separator());

        if (ViewModel.NewFileTemplates.Count == 0)
        {
            menuItem.Items.Add(new MenuItem
            {
                Header = "No Templates Found",
                IsEnabled = false
            });
        }
        else
        {
            foreach (var template in ViewModel.NewFileTemplates)
            {
                menuItem.Items.Add(new MenuItem
                {
                    Header = template.DisplayName,
                    Command = ViewModel.CreateFileFromTemplateCommand,
                    CommandParameter = template
                });
            }
        }

        menuItem.Items.Add(new Separator());
        menuItem.Items.Add(new MenuItem
        {
            Header = "Open Templates Folder",
            Command = ViewModel.OpenNewFileTemplatesFolderCommand
        });
        menuItem.Items.Add(new MenuItem
        {
            Header = "Refresh Templates",
            Command = ViewModel.RefreshNewFileTemplatesCommand
        });
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentPaneFileList != null)
            ViewModel.CurrentPaneFileList.FilterText = string.Empty;
    }

    private void SetViewMode(string mode)
    {
        PersistDetailsColumnLayout(saveImmediately: false);
        if (ViewModel.CurrentPaneFileList != null)
            ViewModel.CurrentPaneFileList.ViewMode = mode;
    }

    private void DetailsColumnsMenu_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        var settings = GetDetailsColumnSettings();
        NameColumnMenuItem.IsChecked = settings.Any(static setting => setting.ColumnId == DetailsColumnId.Name && setting.IsVisible);
        SizeColumnMenuItem.IsChecked = settings.Any(static setting => setting.ColumnId == DetailsColumnId.Size && setting.IsVisible);
        TypeColumnMenuItem.IsChecked = settings.Any(static setting => setting.ColumnId == DetailsColumnId.Type && setting.IsVisible);
        DateModifiedColumnMenuItem.IsChecked = settings.Any(static setting => setting.ColumnId == DetailsColumnId.DateModified && setting.IsVisible);
        CompanyColumnMenuItem.IsChecked = settings.Any(static setting => setting.ColumnId == DetailsColumnId.Company && setting.IsVisible);
        VersionColumnMenuItem.IsChecked = settings.Any(static setting => setting.ColumnId == DetailsColumnId.Version && setting.IsVisible);
        DimensionsColumnMenuItem.IsChecked = settings.Any(static setting => setting.ColumnId == DetailsColumnId.Dimensions && setting.IsVisible);
        DurationColumnMenuItem.IsChecked = settings.Any(static setting => setting.ColumnId == DetailsColumnId.Duration && setting.IsVisible);
    }

    private void DetailsColumnVisibility_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string rawColumnId })
            return;
        if (!Enum.TryParse<DetailsColumnId>(rawColumnId, ignoreCase: true, out var columnId))
            return;

        PersistDetailsColumnLayout(saveImmediately: false);
        var settings = GetDetailsColumnSettings().ToList();
        var target = settings.FirstOrDefault(setting => setting.ColumnId == columnId);
        if (target == null)
            return;

        var visibleCount = settings.Count(static setting => setting.IsVisible);
        if (target.IsVisible && visibleCount == 1)
        {
            DetailsColumnsMenu_SubmenuOpened(sender, e);
            MessageBox.Show(
                "At least one details column must remain visible.",
                "Columns",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        target.IsVisible = !target.IsVisible;
        ViewModel.SaveDetailsColumnSettings(settings);
        ApplyDetailsLayoutToBothPanes();
        DetailsColumnsMenu_SubmenuOpened(sender, e);
    }

    private void AutoSizeVisibleColumns_Click(object sender, RoutedEventArgs e)
    {
        AutoSizeVisibleColumns(FileListView);
        AutoSizeVisibleColumns(RightPaneListView);
        PersistDetailsColumnLayout(saveImmediately: true);
    }

    private void MoveDetailsColumn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string rawTag })
            return;

        var parts = rawTag.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return;
        if (!Enum.TryParse<DetailsColumnId>(parts[0], ignoreCase: true, out var columnId))
            return;
        if (!int.TryParse(parts[1], out var delta))
            return;

        PersistDetailsColumnLayout(saveImmediately: false);
        var settings = GetDetailsColumnSettings().ToList();
        var index = settings.FindIndex(setting => setting.ColumnId == columnId);
        if (index < 0)
            return;

        var targetIndex = Math.Clamp(index + delta, 0, settings.Count - 1);
        if (targetIndex == index)
            return;

        var moved = settings[index];
        settings.RemoveAt(index);
        settings.Insert(targetIndex, moved);
        for (var i = 0; i < settings.Count; i++)
            settings[i].DisplayOrder = i;

        ViewModel.SaveDetailsColumnSettings(settings);
        ApplyDetailsLayoutToBothPanes();
    }

    private void ResetDetailsColumns_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SaveDetailsColumnSettings([]);
        ApplyDetailsLayoutToBothPanes();
        DetailsColumnsMenu_SubmenuOpened(sender, e);
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
        ViewModel.OpenInExplorerCommand.Execute(null);
    }

    private void OpenInNewWindow_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenInNewWindowCommand.Execute(null);
    }

    private void RunAsAdministrator_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RunAsAdministratorCommand.Execute(null);
    }

    private void OnExitClick(object sender, RoutedEventArgs e) => Close();

    private void SessionsMenu_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        if (!ReferenceEquals(sender, e.OriginalSource))
            return;

        PopulateLoadSessionMenu();
        PopulateDeleteSessionMenu();
        PopulateStartupSessionMenu();
    }

    private void SaveCurrentSessionAs_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NewFolderDialog
        {
            Title = "Save Session As",
            Message = "Enter session name:"
        };

        if (dialog.ShowDialog() != true)
            return;

        var sessionName = dialog.FolderName.Trim();
        if (string.IsNullOrWhiteSpace(sessionName))
        {
            MessageBox.Show(
                "Session name cannot be empty.",
                "Session Manager",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var existing = ViewModel.NamedSessions.Any(session =>
            string.Equals(session.Name, sessionName, StringComparison.OrdinalIgnoreCase));
        var overwriteExisting = false;

        if (existing)
        {
            overwriteExisting = MessageBox.Show(
                $"Overwrite the existing session '{sessionName}'?",
                "Session Manager",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes;

            if (!overwriteExisting)
                return;
        }

        if (!ViewModel.SaveNamedSession(sessionName, overwriteExisting))
        {
            MessageBox.Show(
                "Unable to save the session.",
                "Session Manager",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void PopulateLoadSessionMenu()
    {
        LoadSessionMenuItem.Items.Clear();
        if (ViewModel.NamedSessions.Count == 0)
        {
            LoadSessionMenuItem.Items.Add(new MenuItem
            {
                Header = "No Saved Sessions",
                IsEnabled = false
            });
            return;
        }

        foreach (var session in ViewModel.NamedSessions)
        {
            var sessionName = session.Name;
            var item = new MenuItem { Header = sessionName };
            item.Click += (_, _) =>
            {
                if (!ViewModel.LoadNamedSession(sessionName))
                {
                    MessageBox.Show(
                        $"Unable to load session '{sessionName}'.",
                        "Session Manager",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            };
            LoadSessionMenuItem.Items.Add(item);
        }
    }

    private void PopulateDeleteSessionMenu()
    {
        DeleteSessionMenuItem.Items.Clear();
        if (ViewModel.NamedSessions.Count == 0)
        {
            DeleteSessionMenuItem.Items.Add(new MenuItem
            {
                Header = "No Saved Sessions",
                IsEnabled = false
            });
            return;
        }

        foreach (var session in ViewModel.NamedSessions)
        {
            var sessionName = session.Name;
            var item = new MenuItem { Header = sessionName };
            item.Click += (_, _) =>
            {
                var confirmed = MessageBox.Show(
                    $"Delete session '{sessionName}'?",
                    "Session Manager",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (confirmed != MessageBoxResult.Yes)
                    return;

                ViewModel.DeleteNamedSession(sessionName);
            };
            DeleteSessionMenuItem.Items.Add(item);
        }
    }

    private void PopulateStartupSessionMenu()
    {
        StartupSessionMenuItem.Items.Clear();

        var startupSessionName = ViewModel.GetStartupSessionName();
        var autoRestoreItem = new MenuItem
        {
            Header = "Auto Restore Last State",
            IsCheckable = true,
            IsChecked = string.IsNullOrWhiteSpace(startupSessionName)
        };
        autoRestoreItem.Click += (_, _) => ViewModel.SetStartupSession(null);
        StartupSessionMenuItem.Items.Add(autoRestoreItem);

        if (ViewModel.NamedSessions.Count == 0)
            return;

        StartupSessionMenuItem.Items.Add(new Separator());

        foreach (var session in ViewModel.NamedSessions)
        {
            var sessionName = session.Name;
            var item = new MenuItem
            {
                Header = sessionName,
                IsCheckable = true,
                IsChecked = string.Equals(startupSessionName, sessionName, StringComparison.OrdinalIgnoreCase)
            };
            item.Click += (_, _) => ViewModel.SetStartupSession(sessionName);
            StartupSessionMenuItem.Items.Add(item);
        }
    }

    private void DuplicateTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: TabViewModel tab })
        {
            ViewModel.DuplicateTab(tab);
        }
    }

    private void ToggleTabLock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: TabViewModel tab })
        {
            ViewModel.ToggleTabLock(tab);
        }
    }

    private void SetTabColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem)
            return;

        var parent = ItemsControl.ItemsControlFromItemContainer(menuItem);
        while (parent is MenuItem parentMenuItem)
            parent = ItemsControl.ItemsControlFromItemContainer(parentMenuItem);

        var tab = ((parent as ContextMenu)?.PlacementTarget as FrameworkElement)?.DataContext as TabViewModel;
        if (tab == null)
            return;

        ViewModel.SetTabColor(tab, menuItem.Tag as string);
    }

    private void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: TabViewModel tab })
        {
            ViewModel.CloseTabCommand.Execute(tab);
        }
    }

    private void CloseTabsToLeft_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: TabViewModel tab })
        {
            ViewModel.CloseTabsToLeft(tab);
        }
    }

    private void CloseTabsToRight_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: TabViewModel tab })
        {
            ViewModel.CloseTabsToRight(tab);
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

    private void EmptyRecycleBin_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Empty the Recycle Bin for all drives?",
            "Empty Recycle Bin",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (result != MessageBoxResult.Yes)
            return;

        ViewModel.EmptyRecycleBinCommand.Execute(null);
    }

    private void MainWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_boundViewModel != null)
        {
            _boundViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _boundViewModel.Tabs.CollectionChanged -= Tabs_CollectionChanged;
            _boundViewModel.DualPaneModeChanged -= ViewModel_DualPaneModeChanged;
            _boundViewModel.PreviewModeChanged -= ViewModel_PreviewModeChanged;
            _boundViewModel.OpenPreferencesRequested -= ViewModel_OpenPreferencesRequested;
            _boundViewModel.SplitFileRequested -= ViewModel_SplitFileRequested;
            _boundViewModel.JoinFileRequested -= ViewModel_JoinFileRequested;
            _boundViewModel.ChecksumRequested -= ViewModel_ChecksumRequested;
            _boundViewModel.ScrollToSelectedRequested -= MainWindow_ScrollToSelectedRequested;
            _boundViewModel.ScrollToSelectedBookmarkRequested -= MainWindow_ScrollToSelectedBookmarkRequested;
            _boundViewModel.BookmarkPropertiesRequested -= ViewModel_BookmarkPropertiesRequested;
        }

        _boundViewModel = e.NewValue as MainViewModel;
        if (_boundViewModel != null)
        {
            _boundViewModel.PropertyChanged += ViewModel_PropertyChanged;
            _boundViewModel.Tabs.CollectionChanged += Tabs_CollectionChanged;
            _boundViewModel.DualPaneModeChanged += ViewModel_DualPaneModeChanged;
            _boundViewModel.PreviewModeChanged += ViewModel_PreviewModeChanged;
            _boundViewModel.OpenPreferencesRequested += ViewModel_OpenPreferencesRequested;
            _boundViewModel.SplitFileRequested += ViewModel_SplitFileRequested;
            _boundViewModel.JoinFileRequested += ViewModel_JoinFileRequested;
            _boundViewModel.ChecksumRequested += ViewModel_ChecksumRequested;
            _boundViewModel.ScrollToSelectedRequested += MainWindow_ScrollToSelectedRequested;
            _boundViewModel.ScrollToSelectedBookmarkRequested += MainWindow_ScrollToSelectedBookmarkRequested;
            _boundViewModel.BookmarkPropertiesRequested += ViewModel_BookmarkPropertiesRequested;
            HookFileListViewModel(_boundViewModel.ActiveTab?.FileList);
            HookRightFileListViewModel(_boundViewModel.RightPaneTab?.FileList);
            ApplyDetailsLayoutToBothPanes();
            UpdatePaneHighlight();
            QueueTabStripUpdate(scrollActiveIntoView: true);
            QueueFilePaneVisibilityCheck();
        }
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        PersistDetailsColumnLayout(saveImmediately: true);
    }

    private void MainWindow_ScrollToSelectedRequested(object? sender, FolderTreeNodeViewModel node)
    {
        // We might need a few attempts as containers are generated
        int attempts = 0;
        void TryScroll()
        {
            var selectedContainer = FindSelectedTreeViewItem(FolderTree);
            if (selectedContainer != null)
            {
                selectedContainer.BringIntoView();
            }
            else if (attempts < 10)
            {
                attempts++;
                Dispatcher.BeginInvoke(new Action(TryScroll), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        Dispatcher.BeginInvoke(new Action(TryScroll), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void MainWindow_ScrollToSelectedBookmarkRequested(object? sender, BookmarkItem bookmark)
    {
        int attempts = 0;
        void TryScroll()
        {
            var container = FindTreeViewItemByData(BookmarkTree, bookmark);
            if (container != null)
            {
                container.IsSelected = true;
                container.BringIntoView();
            }
            else if (attempts < 10)
            {
                attempts++;
                Dispatcher.BeginInvoke(new Action(TryScroll), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        Dispatcher.BeginInvoke(new Action(TryScroll), System.Windows.Threading.DispatcherPriority.Background);
    }

    private TreeViewItem? FindSelectedTreeViewItem(ItemsControl parent)
    {
        // Try to find in current items
        foreach (var item in parent.Items)
        {
            var container = parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
            if (container == null) continue;

            if (container.IsSelected)
                return container;

            // If it's expanded, we can look deeper
            if (container.IsExpanded)
            {
                var result = FindSelectedTreeViewItem(container);
                if (result != null)
                    return result;
            }
        }
        return null;
    }

    private TreeViewItem? FindTreeViewItemByData(ItemsControl parent, object targetItem)
    {
        foreach (var item in parent.Items)
        {
            var container = parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
            if (container == null) continue;

            if (ReferenceEquals(item, targetItem))
                return container;

            var result = FindTreeViewItemByData(container, targetItem);
            if (result != null)
                return result;
        }

        return null;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.ActiveTab))
        {
            HookFileListViewModel(_boundViewModel?.ActiveTab?.FileList);
            QueueTabStripUpdate(scrollActiveIntoView: true);
            QueueFilePaneVisibilityCheck();
        }
        else if (e.PropertyName == nameof(MainViewModel.RightPaneTab))
        {
            HookRightFileListViewModel(_boundViewModel?.RightPaneTab?.FileList);
            QueueFilePaneVisibilityCheck();
        }
        else if (e.PropertyName == nameof(MainViewModel.DetailsColumnSettings))
        {
            ApplyDetailsLayoutToBothPanes();
        }

        if (e.PropertyName == nameof(MainViewModel.IsRightPaneActive)
            || e.PropertyName == nameof(MainViewModel.IsDualPaneMode))
        {
            UpdatePaneHighlight();
        }
    }

    private void Tabs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        QueueTabStripUpdate(scrollActiveIntoView: true);
    }

    private void EnsureTabStripParts()
    {
        if (_tabHeaderScrollViewer != null && _tabScrollLeftButton != null
            && _tabScrollRightButton != null && _tabOverflowButton != null && _tabOverflowMenu != null)
            return;

        TabsControl.ApplyTemplate();
        _tabHeaderScrollViewer = TabsControl.Template.FindName("TabHeaderScrollViewer", TabsControl) as ScrollViewer;
        _tabScrollLeftButton = TabsControl.Template.FindName("TabScrollLeftButton", TabsControl) as Button;
        _tabScrollRightButton = TabsControl.Template.FindName("TabScrollRightButton", TabsControl) as Button;
        _tabOverflowButton = TabsControl.Template.FindName("TabOverflowButton", TabsControl) as Button;
        _tabOverflowMenu = _tabOverflowButton?.ContextMenu;

        if (_tabHeaderScrollViewer != null)
        {
            _tabHeaderScrollViewer.ScrollChanged -= TabHeaderScrollViewer_ScrollChanged;
            _tabHeaderScrollViewer.ScrollChanged += TabHeaderScrollViewer_ScrollChanged;
        }
    }

    private void TabHeaderScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        UpdateTabStripAffordances();
    }

    private void QueueTabStripUpdate(bool scrollActiveIntoView)
    {
        Dispatcher.BeginInvoke(() =>
        {
            EnsureTabStripParts();
            UpdateTabStripAffordances();
            if (scrollActiveIntoView)
                EnsureActiveTabVisible();
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void QueueFilePaneVisibilityCheck()
    {
        Dispatcher.BeginInvoke(() =>
        {
            EnsureVisibleFileRows(FileListView, _boundFileListViewModel);
            EnsureVisibleFileRows(RightPaneListView, _boundRightFileListViewModel);
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void EnsureVisibleFileRows(ListView listView, FileListViewModel? fileListViewModel)
    {
        if (fileListViewModel == null || listView.Items.Count == 0)
            return;

        listView.UpdateLayout();
        listView.ScrollIntoView(listView.Items[0]);
        listView.UpdateLayout();

        if (listView.ItemContainerGenerator.ContainerFromIndex(0) == null
            && string.Equals(fileListViewModel.ViewMode, "Details", StringComparison.OrdinalIgnoreCase))
        {
            fileListViewModel.ViewMode = "List";
            listView.UpdateLayout();
        }
    }

    private void UpdateTabStripAffordances()
    {
        EnsureTabStripParts();
        if (_tabHeaderScrollViewer == null || _tabScrollLeftButton == null
            || _tabScrollRightButton == null || _tabOverflowButton == null)
            return;

        _tabHeaderScrollViewer.UpdateLayout();
        var hasOverflow = _tabHeaderScrollViewer.ScrollableWidth > 1;
        var overflowVisibility = hasOverflow ? Visibility.Visible : Visibility.Collapsed;
        _tabScrollLeftButton.Visibility = overflowVisibility;
        _tabScrollRightButton.Visibility = overflowVisibility;
        _tabOverflowButton.Visibility = overflowVisibility;

        _tabScrollLeftButton.IsEnabled = hasOverflow && _tabHeaderScrollViewer.HorizontalOffset > 0.5;
        _tabScrollRightButton.IsEnabled = hasOverflow
            && _tabHeaderScrollViewer.HorizontalOffset < _tabHeaderScrollViewer.ScrollableWidth - 0.5;
    }

    private void EnsureActiveTabVisible()
    {
        EnsureTabStripParts();
        if (_tabHeaderScrollViewer == null || _boundViewModel?.ActiveTab == null)
            return;

        TabsControl.UpdateLayout();
        var container = TabsControl.ItemContainerGenerator.ContainerFromItem(_boundViewModel.ActiveTab) as System.Windows.Controls.TabItem;
        if (container == null)
            return;

        var bounds = container.TransformToAncestor(_tabHeaderScrollViewer)
            .TransformBounds(new Rect(new Point(), container.RenderSize));

        if (bounds.Left < 0)
        {
            _tabHeaderScrollViewer.ScrollToHorizontalOffset(Math.Max(0, _tabHeaderScrollViewer.HorizontalOffset + bounds.Left - 8));
        }
        else if (bounds.Right > _tabHeaderScrollViewer.ViewportWidth)
        {
            var delta = bounds.Right - _tabHeaderScrollViewer.ViewportWidth + 8;
            _tabHeaderScrollViewer.ScrollToHorizontalOffset(
                Math.Min(_tabHeaderScrollViewer.ScrollableWidth, _tabHeaderScrollViewer.HorizontalOffset + delta));
        }

        UpdateTabStripAffordances();
    }

    private void ScrollTabStrip(double direction)
    {
        EnsureTabStripParts();
        if (_tabHeaderScrollViewer == null)
            return;

        var pageWidth = _tabHeaderScrollViewer.ViewportWidth > 0
            ? _tabHeaderScrollViewer.ViewportWidth * 0.65
            : 180;
        var targetOffset = _tabHeaderScrollViewer.HorizontalOffset + (pageWidth * direction);
        targetOffset = Math.Max(0, Math.Min(_tabHeaderScrollViewer.ScrollableWidth, targetOffset));
        _tabHeaderScrollViewer.ScrollToHorizontalOffset(targetOffset);
        UpdateTabStripAffordances();
    }

    private void TabScrollLeftButton_Click(object sender, RoutedEventArgs e)
    {
        ScrollTabStrip(-1);
    }

    private void TabScrollRightButton_Click(object sender, RoutedEventArgs e)
    {
        ScrollTabStrip(1);
    }

    private void TabOverflowButton_Click(object sender, RoutedEventArgs e)
    {
        EnsureTabStripParts();
        if (sender is not Button button || _tabOverflowMenu == null)
            return;

        PopulateTabOverflowMenu(_tabOverflowMenu);
        if (_tabOverflowMenu.Items.Count == 0)
            return;

        _tabOverflowMenu.PlacementTarget = button;
        _tabOverflowMenu.Placement = PlacementMode.Bottom;
        _tabOverflowMenu.IsOpen = true;
    }

    private void PopulateTabOverflowMenu(ContextMenu menu)
    {
        menu.Items.Clear();
        if (_boundViewModel == null)
            return;

        foreach (var tab in _boundViewModel.Tabs)
        {
            var item = new MenuItem
            {
                Header = string.IsNullOrWhiteSpace(tab.Title) ? "(Untitled)" : tab.Title,
                ToolTip = tab.CurrentPath,
                IsCheckable = true,
                IsChecked = ReferenceEquals(tab, _boundViewModel.ActiveTab),
                Tag = tab
            };
            item.Click += TabOverflowItem_Click;
            menu.Items.Add(item);
        }
    }

    private void TabOverflowItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: TabViewModel tab } || _boundViewModel == null)
            return;

        _boundViewModel.ActiveTab = tab;
        QueueTabStripUpdate(scrollActiveIntoView: true);
    }

    private void HookFileListViewModel(FileListViewModel? fileListViewModel)
        => HookPaneFileListViewModel(ref _boundFileListViewModel, fileListViewModel, FileListView);

    private void HookRightFileListViewModel(FileListViewModel? fileListViewModel)
        => HookPaneFileListViewModel(ref _boundRightFileListViewModel, fileListViewModel, RightPaneListView);

    private void HookPaneFileListViewModel(
        ref FileListViewModel? boundField,
        FileListViewModel? newViewModel,
        ListView targetListView)
    {
        if (boundField != null)
        {
            boundField.SelectAllRequested -= FileListViewModel_SelectAllRequested;
            boundField.InvertSelectionRequested -= FileListViewModel_InvertSelectionRequested;
            boundField.InlineRenameRequested -= FileListViewModel_InlineRenameRequested;
            boundField.ViewModeChanged -= FileListViewModel_ViewModeChanged;
            boundField.PropertiesRequested -= FileListViewModel_PropertiesRequested;
            boundField.SearchPanelOpened -= FileListViewModel_SearchPanelOpened;
            boundField.ArchiveBrowseRequested -= FileListViewModel_ArchiveBrowseRequested;
        }

        boundField = newViewModel;
        if (boundField != null)
        {
            boundField.SelectAllRequested += FileListViewModel_SelectAllRequested;
            boundField.InvertSelectionRequested += FileListViewModel_InvertSelectionRequested;
            boundField.InlineRenameRequested += FileListViewModel_InlineRenameRequested;
            boundField.ViewModeChanged += FileListViewModel_ViewModeChanged;
            boundField.PropertiesRequested += FileListViewModel_PropertiesRequested;
            boundField.SearchPanelOpened += FileListViewModel_SearchPanelOpened;
            boundField.ArchiveBrowseRequested += FileListViewModel_ArchiveBrowseRequested;
            ApplyViewMode(targetListView, boundField.ViewMode);
        }
    }

    private void FileListViewModel_SelectAllRequested(object? sender, EventArgs e)
    {
        if (sender == _boundRightFileListViewModel)
        {
            RightPaneListView.SelectAll();
        }
        else
        {
            FileListView.SelectAll();
        }
    }

    private void FileListViewModel_InvertSelectionRequested(object? sender, EventArgs e)
    {
        var targetListView = sender == _boundRightFileListViewModel ? RightPaneListView : FileListView;
        var selected = targetListView.SelectedItems.Cast<object>().ToList();
        targetListView.SelectedItems.Clear();
        foreach (var item in targetListView.Items)
        {
            if (!selected.Contains(item))
            {
                targetListView.SelectedItems.Add(item);
            }
        }
    }

    private void FileListViewModel_InlineRenameRequested(object? sender, FileSystemItem item)
    {
        if (sender == _boundRightFileListViewModel)
        {
            BeginInlineRename(item, RightPaneListView, _boundRightFileListViewModel);
        }
        else
        {
            BeginInlineRename(item, FileListView, _boundFileListViewModel);
        }
    }

    private void FileListViewModel_ViewModeChanged(object? sender, string mode)
    {
        if (sender == _boundRightFileListViewModel)
        {
            ApplyViewMode(RightPaneListView, mode);
        }
        else
        {
            ApplyViewMode(FileListView, mode);
        }
    }

    private void FileListViewModel_PropertiesRequested(object? sender, FileSystemItem item)
    {
        if (ViewModel.CurrentSettings.UseShellContextMenu)
        {
            var selectedItems = (sender as FileListViewModel)?.SelectedItems;
            if (selectedItems != null && selectedItems.Count > 1)
            {
                ViewModel.FileSystemService.ShowNativeProperties(selectedItems.Select(static i => i.FullPath));
                return;
            }
            
            ViewModel.FileSystemService.ShowNativeProperties(item.FullPath);
            return;
        }

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

    private void FileListViewModel_ArchiveBrowseRequested(object? sender, ArchiveBrowseRequest request)
    {
        var dialog = new ArchiveBrowserDialog(request.ArchivePath, request.Entries, request.SourceFileList) { Owner = this };
        dialog.ShowDialog();
    }

    private void ApplyViewMode(ListView listView, string mode)
    {
        switch (mode)
        {
            case "Details":
                listView.ClearValue(ItemsControl.ItemTemplateProperty);
                listView.ClearValue(ItemsControl.ItemsPanelProperty);
                if (listView.View is GridView existingGridView)
                {
                    PopulateDetailsColumns(existingGridView);
                }
                else
                {
                    listView.View = CreateDetailsGridView();
                }
                break;
            case "List":
                listView.View = null;
                listView.ItemTemplate = FindApplicationResource<DataTemplate>("ListViewItemTemplate");
                listView.ClearValue(ItemsControl.ItemsPanelProperty);
                break;
            case "Tiles":
                listView.View = null;
                listView.ItemTemplate = FindApplicationResource<DataTemplate>("TileViewItemTemplate");
                listView.ItemsPanel = FindApplicationResource<ItemsPanelTemplate>("TileItemsPanelTemplate");
                break;
        }
    }

    private GridView CreateDetailsGridView()
    {
        var gridView = new GridView();
        PopulateDetailsColumns(gridView);
        return gridView;
    }

    private void PopulateDetailsColumns(GridView gridView)
    {
        gridView.Columns.Clear();
        foreach (var setting in GetDetailsColumnSettings().Where(static setting => setting.IsVisible))
            gridView.Columns.Add(CreateDetailsColumn(setting));
    }

    private GridViewColumn CreateDetailsColumn(DetailsColumnSetting setting)
    {
        return setting.ColumnId switch
        {
            DetailsColumnId.Name => new GridViewColumn
            {
                Header = "Name",
                Width = setting.Width,
                CellTemplate = FindApplicationResource<DataTemplate>("DetailsNameCellTemplate")
            },
            DetailsColumnId.Size => new GridViewColumn
            {
                Header = "Size",
                Width = setting.Width,
                DisplayMemberBinding = new System.Windows.Data.Binding("DisplaySize")
            },
            DetailsColumnId.Type => new GridViewColumn
            {
                Header = "Type",
                Width = setting.Width,
                DisplayMemberBinding = new System.Windows.Data.Binding("TypeDescription")
            },
            DetailsColumnId.DateModified => new GridViewColumn
            {
                Header = "Date Modified",
                Width = setting.Width,
                DisplayMemberBinding = new System.Windows.Data.Binding("DateModified")
                {
                    StringFormat = "{0:yyyy-MM-dd HH:mm}"
                }
            },
            DetailsColumnId.Company => new GridViewColumn
            {
                Header = "Company",
                Width = setting.Width,
                DisplayMemberBinding = new System.Windows.Data.Binding("ShellProperties.Company")
            },
            DetailsColumnId.Version => new GridViewColumn
            {
                Header = "Version",
                Width = setting.Width,
                DisplayMemberBinding = new System.Windows.Data.Binding("ShellProperties.FileVersion")
            },
            DetailsColumnId.Dimensions => new GridViewColumn
            {
                Header = "Dimensions",
                Width = setting.Width,
                DisplayMemberBinding = new System.Windows.Data.Binding("ShellProperties.Dimensions")
            },
            DetailsColumnId.Duration => new GridViewColumn
            {
                Header = "Duration",
                Width = setting.Width,
                DisplayMemberBinding = new System.Windows.Data.Binding("ShellProperties.Duration")
            },
            _ => throw new InvalidOperationException($"Unsupported details column: {setting.ColumnId}")
        };
    }

    private IReadOnlyList<DetailsColumnSetting> GetDetailsColumnSettings()
    {
        return _boundViewModel?.GetDetailsColumnSettings()
            ?? ViewModel.GetDetailsColumnSettings();
    }

    private static T FindApplicationResource<T>(object key) where T : class
    {
        var resource = Application.Current?.TryFindResource(key) as T;
        if (resource != null)
            return resource;

        throw new InvalidOperationException($"Required resource '{key}' was not found.");
    }

    private void ApplyDetailsLayoutToBothPanes()
    {
        if (_isApplyingDetailsLayout)
            return;

        try
        {
            _isApplyingDetailsLayout = true;
            if (_boundFileListViewModel?.ViewMode == "Details")
                ApplyViewMode(FileListView, "Details");
            if (_boundRightFileListViewModel?.ViewMode == "Details")
                ApplyViewMode(RightPaneListView, "Details");
        }
        finally
        {
            _isApplyingDetailsLayout = false;
        }
    }

    private void PersistDetailsColumnLayout(bool saveImmediately)
    {
        if (_isApplyingDetailsLayout || _boundViewModel == null)
            return;

        var snapshot = CaptureDetailsColumnSettings(FileListView)
            ?? CaptureDetailsColumnSettings(RightPaneListView);
        if (snapshot == null)
            return;

        if (saveImmediately)
        {
            _boundViewModel.SaveDetailsColumnSettings(snapshot);
            return;
        }

        _boundViewModel.CurrentSettings.DetailsColumns = snapshot.ToList();
    }

    private IReadOnlyList<DetailsColumnSetting>? CaptureDetailsColumnSettings(ListView listView)
    {
        if (listView.View is not GridView gridView)
            return null;
        if (!ReferenceEquals(listView.ItemTemplate, null) || listView.ItemsPanel != null)
            return null;

        var existing = GetDetailsColumnSettings()
            .ToDictionary(static setting => setting.ColumnId, static setting => new DetailsColumnSetting
            {
                ColumnId = setting.ColumnId,
                Width = setting.Width,
                IsVisible = setting.IsVisible,
                DisplayOrder = setting.DisplayOrder
            });

        var visibleOrder = 0;
        foreach (var column in gridView.Columns)
        {
            if (!TryGetColumnId(column, out var columnId))
                continue;

            existing[columnId] = new DetailsColumnSetting
            {
                ColumnId = columnId,
                Width = GetPersistedColumnWidth(column, existing[columnId].Width),
                IsVisible = true,
                DisplayOrder = visibleOrder++
            };
        }

        foreach (var columnId in DetailsColumnOrder)
        {
            if (!existing.TryGetValue(columnId, out var current))
                continue;
            if (current.IsVisible)
                continue;

            current.DisplayOrder = visibleOrder++;
        }

        return existing.Values
            .OrderBy(static setting => setting.DisplayOrder)
            .ToList();
    }

    private static double GetPersistedColumnWidth(GridViewColumn column, double fallbackWidth)
    {
        if (!double.IsNaN(column.Width) && column.Width > 24)
            return column.Width;

        return column.ActualWidth > 24 ? column.ActualWidth : fallbackWidth;
    }

    private static bool TryGetColumnId(GridViewColumn column, out DetailsColumnId columnId)
    {
        var headerText = column.Header?.ToString()?.TrimEnd(" \u25B2\u25BC".ToCharArray());
        return headerText switch
        {
            "Name" => SetColumnId(DetailsColumnId.Name, out columnId),
            "Size" => SetColumnId(DetailsColumnId.Size, out columnId),
            "Type" => SetColumnId(DetailsColumnId.Type, out columnId),
            "Date Modified" => SetColumnId(DetailsColumnId.DateModified, out columnId),
            "Company" => SetColumnId(DetailsColumnId.Company, out columnId),
            "Version" => SetColumnId(DetailsColumnId.Version, out columnId),
            "Dimensions" => SetColumnId(DetailsColumnId.Dimensions, out columnId),
            "Duration" => SetColumnId(DetailsColumnId.Duration, out columnId),
            _ => SetColumnId(default, out columnId, false)
        };
    }

    private static bool SetColumnId(DetailsColumnId id, out DetailsColumnId columnId, bool result = true)
    {
        columnId = id;
        return result;
    }

    private void AutoSizeVisibleColumns(ListView listView)
    {
        if (listView.View is not GridView gridView)
            return;

        listView.UpdateLayout();
        foreach (var column in gridView.Columns)
        {
            var currentWidth = column.Width;
            column.Width = double.NaN;
            listView.UpdateLayout();
            if (double.IsNaN(column.ActualWidth) || column.ActualWidth <= 24)
                column.Width = currentWidth;
            else
                column.Width = column.ActualWidth;
        }
    }

    private void BeginInlineRename(FileSystemItem item, ListView listView, FileListViewModel? fileListViewModel)
    {
        if (item == null || fileListViewModel == null) return;

        listView.ScrollIntoView(item);
        listView.UpdateLayout();

        var container = listView.ItemContainerGenerator.ContainerFromItem(item) as ListViewItem;
        if (container == null) return;

        var nameTextBlock = FindVisualChildren<TextBlock>(container)
            .FirstOrDefault(tb => tb.Name == "FileNameTextBlock");
        if (nameTextBlock == null) return;

        var point = nameTextBlock.TranslatePoint(new Point(0, 0), listView);
        InlineRenamePopup.PlacementTarget = listView;
        InlineRenamePopup.HorizontalOffset = Math.Max(0, point.X - 2);
        InlineRenamePopup.VerticalOffset = Math.Max(0, point.Y - 1);
        InlineRenameTextBox.Width = Math.Max(180, nameTextBlock.ActualWidth + 18);
        InlineRenameTextBox.Text = item.Name;
        InlineRenamePopup.IsOpen = true;
        _inlineRenameItem = item;
        _inlineRenameFileListViewModel = fileListViewModel;
        _inlineRenameListView = listView;

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
        var fileList = _inlineRenameFileListViewModel;
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
        _inlineRenameFileListViewModel = null;
        _inlineRenameListView = null;
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

    // --- Dual Pane ---

    private void ViewModel_DualPaneModeChanged(object? sender, EventArgs e)
    {
        var enabled = _boundViewModel?.IsDualPaneMode == true;
        DualPaneSplitterCol.Width = enabled ? GridLength.Auto : new GridLength(0);
        DualPaneCol.Width = enabled ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        UpdatePaneHighlight();
    }

    private void ViewModel_PreviewModeChanged(object? sender, EventArgs e)
    {
        var enabled = _boundViewModel?.IsPreviewVisible == true;
        PreviewSplitterCol.Width = enabled ? GridLength.Auto : new GridLength(0);
        PreviewCol.Width = enabled ? new GridLength(280) : new GridLength(0);
    }

    private void ViewModel_OpenPreferencesRequested(object? sender, EventArgs e)
    {
        var dialog = new PreferencesWindow(ViewModel.CurrentSettings);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            ViewModel.ApplyAndSaveSettings(dialog.Settings);
        }
    }

    private async void ViewModel_SplitFileRequested(object? sender, string? initialPath)
    {
        var dialog = new SplitFileDialog(initialPath) { Owner = this };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            await ViewModel.SplitFileAsync(dialog.SourcePath, dialog.ChunkSizeBytes, dialog.OutputDirectory);
        }
        catch (OperationCanceledException)
        {
            // Status text already reflects cancellation.
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Split failed: {ex.Message}", "Split File", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ViewModel_JoinFileRequested(object? sender, string? initialPath)
    {
        var dialog = new JoinFileDialog(initialPath) { Owner = this };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            await ViewModel.JoinFileAsync(dialog.FirstChunkPath, dialog.OutputPath);
        }
        catch (OperationCanceledException)
        {
            // Status text already reflects cancellation.
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Join failed: {ex.Message}", "Join File", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ViewModel_ChecksumRequested(object? sender, string? initialPath)
    {
        var dialog = new ChecksumDialog(initialPath, ViewModel.ComputeChecksumsAsync) { Owner = this };
        dialog.ShowDialog();
    }

    private void ViewModel_BookmarkPropertiesRequested(object? sender, FileSystemItem item)
    {
        if (ViewModel.CurrentSettings.UseShellContextMenu && !string.IsNullOrWhiteSpace(item.FullPath))
        {
            if (Path.IsPathRooted(item.FullPath) && (File.Exists(item.FullPath) || Directory.Exists(item.FullPath)))
            {
                ViewModel.FileSystemService.ShowNativeProperties(item.FullPath);
                return;
            }
        }

        var dialog = new Views.PropertiesDialog(item);
        dialog.Owner = this;
        dialog.Show();
    }

    private void RightPane_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ViewModel.ActivateRightPane();
        OpenSelectedInPane(ViewModel.RightPaneTab?.FileList);
    }

    private static void OpenSelectedInPane(FileListViewModel? fileList)
    {
        if (fileList?.SelectedItem is { } item)
            fileList.OpenItemCommand.Execute(item);
    }

    private void RightPane_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.ActivateRightPane();
        SyncSelection(RightPaneListView, ViewModel.RightPaneTab?.FileList);
    }

    private void SyncSelection(ListView listView, FileListViewModel? fileList)
    {
        if (fileList == null) return;

        fileList.SelectedItems.Clear();
        foreach (var selected in listView.SelectedItems.OfType<FileSystemItem>())
        {
            fileList.SelectedItems.Add(selected);
        }

        fileList.UpdateSelectionStatus();
    }

    private void RightPane_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        ViewModel.ActivateRightPane();
    }

    private void RightPane_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        ViewModel.ActivateRightPane();
        _rightPaneDragStartPoint = e.GetPosition(RightPaneListView);
    }

    private void RightPane_MouseMove(object sender, MouseEventArgs e)
    {
        TryInitiateDrag(e, RightPaneListView, _rightPaneDragStartPoint, ViewModel.RightPaneTab?.FileList);
    }

    private void TryInitiateDrag(MouseEventArgs e, ListView listView, Point dragStart, FileListViewModel? fileList)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var delta = e.GetPosition(listView) - dragStart;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        if (fileList == null) return;

        var paths = fileList.GetSelectedPathsForTransfer();
        if (paths.Count == 0) return;

        var dropList = new System.Collections.Specialized.StringCollection();
        dropList.AddRange(paths.ToArray());

        var data = new DataObject();
        data.SetFileDropList(dropList);
        data.SetData(InternalDragFormat, true);

        DragDrop.DoDragDrop(listView, data, DragDropEffects.Copy | DragDropEffects.Move);
    }

    private void RightPane_DragEnter(object sender, DragEventArgs e) => UpdateDragEffects(e);

    private void RightPane_DragOver(object sender, DragEventArgs e)
    {
        UpdateDragEffects(e);
        e.Handled = true;
    }

    private async void RightPane_Drop(object sender, DragEventArgs e)
    {
        await HandleDropAsync(e, ViewModel.RightPaneTab?.FileList);
    }

    private void BookmarksBar_DragEnter(object sender, DragEventArgs e)
    {
        UpdateBookmarksBarDragState(e);
    }

    private void BookmarksBar_DragOver(object sender, DragEventArgs e)
    {
        UpdateBookmarksBarDragState(e);
        e.Handled = true;
    }

    private void BookmarksBar_DragLeave(object sender, DragEventArgs e)
    {
        SetBookmarksBarDropHighlight(false);
    }

    private void BookmarksBar_Drop(object sender, DragEventArgs e)
    {
        var droppedDirectories = GetDroppedDirectories(e).ToList();
        foreach (var path in droppedDirectories)
            ViewModel.AddBookmarkFromPath(path, null);

        SetBookmarksBarDropHighlight(false);
        e.Effects = droppedDirectories.Count > 0 ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void UpdateBookmarksBarDragState(DragEventArgs e)
    {
        var hasDroppedDirectories = GetDroppedDirectories(e).Any();
        e.Effects = hasDroppedDirectories ? DragDropEffects.Copy : DragDropEffects.None;
        SetBookmarksBarDropHighlight(hasDroppedDirectories);
    }

    private IEnumerable<string> GetDroppedDirectories(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return [];

        var droppedPaths = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (droppedPaths == null || droppedPaths.Length == 0)
            return [];

        return droppedPaths
            .Where(path => !string.IsNullOrWhiteSpace(path) && ViewModel.FileSystemService.DirectoryExists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private void SetBookmarksBarDropHighlight(bool isActive)
    {
        if (BookmarksBarDropZone == null)
            return;

        BookmarksBarDropZone.BorderBrush = isActive
            ? new SolidColorBrush(Color.FromRgb(0xD3, 0x9B, 0x1A))
            : (Brush)FindResource("ClassicPaneBorderBrush");
        BookmarksBarDropZone.Background = isActive
            ? new SolidColorBrush(Color.FromRgb(0xFF, 0xF8, 0xDD))
            : new SolidColorBrush(Color.FromRgb(0xF5, 0xF6, 0xF8));
    }

    private async Task HandleDropAsync(DragEventArgs e, FileListViewModel? fileList)
    {
        if (fileList == null || !e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var droppedPaths = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (droppedPaths == null || droppedPaths.Length == 0)
            return;

        var destination = ResolveDropDestination(e.OriginalSource as DependencyObject) ?? fileList.CurrentPath;
        var moveFiles = ShouldMove(e);

        await fileList.ImportDroppedFilesAsync(droppedPaths, destination, moveFiles);
        e.Handled = true;
    }

    private void UpdatePaneHighlight()
    {
        var rightPaneVisible = _boundViewModel?.IsDualPaneMode == true;
        var rightActive = _boundViewModel?.IsRightPaneActive == true && rightPaneVisible;

        FileListView.BorderBrush = rightActive ? InactivePaneBrush : ActivePaneBrush;
        FileListView.BorderThickness = rightActive ? new Thickness(1) : new Thickness(2);

        RightPaneListView.BorderBrush = rightActive ? ActivePaneBrush : InactivePaneBrush;
        RightPaneListView.BorderThickness = rightPaneVisible
            ? (rightActive ? new Thickness(2) : new Thickness(1))
            : new Thickness(1);

        RightPaneHeader.BorderBrush = rightActive ? ActivePaneBrush : InactivePaneBrush;
        RightPaneHeader.Background = rightActive
            ? ActiveHeaderGradient
            : (Brush)FindResource("ClassicHeaderBrush");
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
