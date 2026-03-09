using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using CubicAIExplorer.Models;
using CubicAIExplorer.ViewModels;
using CubicAIExplorer.Views;

namespace CubicAIExplorer;

public partial class MainWindow : Window
{
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
    private Point _dragStartPoint;
    private Point _rightPaneDragStartPoint;
    private bool _suppressAutoComplete;
    private bool _suppressRightPaneAutoComplete;

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
            ViewModel.NavigateCurrentPaneToPath(path);
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
            lb.SelectedItem = null; // Reset selection so clicking the same item again works
        }
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

    private void FileList_Drop(object sender, DragEventArgs e)
    {
        HandleDrop(e, ViewModel.ActiveTab?.FileList);
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

    private void FileList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        ViewModel.ActivateLeftPane();
        ConfigureContextMenu(e, ViewModel.ActiveTab?.FileList,
            OpenMenuItem, ItemSeparator1, CutMenuItem, CopyMenuItem, ItemSeparator2,
            DeleteMenuItem, RenameMenuItem, NewFolderMenuItem, RefreshMenuItem,
            PasteMenuItem, PropertiesSeparator, PropertiesMenuItem, OpenInExplorerMenuItem);
    }

    private void RightPane_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        ViewModel.ActivateRightPane();
        ConfigureContextMenu(e, ViewModel.RightPaneTab?.FileList,
            RightOpenMenuItem, RightItemSeparator1, RightCutMenuItem, RightCopyMenuItem, RightItemSeparator2,
            RightDeleteMenuItem, RightRenameMenuItem, RightNewFolderMenuItem, RightRefreshMenuItem,
            RightPasteMenuItem, RightPropertiesSeparator, RightPropertiesMenuItem, RightOpenInExplorerMenuItem);
    }

    private void ConfigureContextMenu(
        ContextMenuEventArgs e, FileListViewModel? fileList,
        FrameworkElement open, FrameworkElement sep1, FrameworkElement cut, FrameworkElement copy,
        FrameworkElement sep2, FrameworkElement delete, FrameworkElement rename,
        FrameworkElement newFolder, FrameworkElement refresh, FrameworkElement paste,
        FrameworkElement propsSep, FrameworkElement props, FrameworkElement openInExplorer)
    {
        if (fileList == null) return;

        var originalSource = e.OriginalSource as DependencyObject;
        var clickedItem = FindVisualParent<ListViewItem>(originalSource);
        var hasSelection = clickedItem != null
            && (fileList.SelectedItems.Count > 0 || fileList.SelectedItem != null);

        var itemVisibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
        var emptyVisibility = hasSelection ? Visibility.Collapsed : Visibility.Visible;

        open.Visibility = itemVisibility;
        sep1.Visibility = itemVisibility;
        cut.Visibility = itemVisibility;
        copy.Visibility = itemVisibility;
        sep2.Visibility = itemVisibility;
        delete.Visibility = itemVisibility;
        rename.Visibility = itemVisibility;

        newFolder.Visibility = emptyVisibility;
        refresh.Visibility = emptyVisibility;
        paste.Visibility = Visibility.Visible;
        propsSep.Visibility = Visibility.Visible;
        props.Visibility = itemVisibility;
        openInExplorer.Visibility = Visibility.Visible;
    }

    private void ViewMode_Details_Click(object sender, RoutedEventArgs e) => SetViewMode("Details");
    private void ViewMode_List_Click(object sender, RoutedEventArgs e) => SetViewMode("List");
    private void ViewMode_Tiles_Click(object sender, RoutedEventArgs e) => SetViewMode("Tiles");

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CurrentFilterText = string.Empty;
    }

    private void SetViewMode(string mode)
    {
        ViewModel.CurrentViewMode = mode;
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
        var path = ViewModel.CurrentPanePath;
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
            _boundViewModel.DualPaneModeChanged -= ViewModel_DualPaneModeChanged;
            _boundViewModel.PreviewModeChanged -= ViewModel_PreviewModeChanged;
        }

        _boundViewModel = e.NewValue as MainViewModel;
        if (_boundViewModel != null)
        {
            _boundViewModel.PropertyChanged += ViewModel_PropertyChanged;
            _boundViewModel.DualPaneModeChanged += ViewModel_DualPaneModeChanged;
            _boundViewModel.PreviewModeChanged += ViewModel_PreviewModeChanged;
            HookFileListViewModel(_boundViewModel.ActiveTab?.FileList);
            HookRightFileListViewModel(_boundViewModel.RightPaneTab?.FileList);
            UpdatePaneHighlight();
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.ActiveTab))
        {
            HookFileListViewModel(_boundViewModel?.ActiveTab?.FileList);
        }
        else if (e.PropertyName == nameof(MainViewModel.RightPaneTab))
        {
            HookRightFileListViewModel(_boundViewModel?.RightPaneTab?.FileList);
        }

        if (e.PropertyName == nameof(MainViewModel.IsRightPaneActive)
            || e.PropertyName == nameof(MainViewModel.IsDualPaneMode))
        {
            UpdatePaneHighlight();
        }
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
            boundField.InlineRenameRequested -= FileListViewModel_InlineRenameRequested;
            boundField.ViewModeChanged -= FileListViewModel_ViewModeChanged;
            boundField.PropertiesRequested -= FileListViewModel_PropertiesRequested;
            boundField.SearchPanelOpened -= FileListViewModel_SearchPanelOpened;
        }

        boundField = newViewModel;
        if (boundField != null)
        {
            boundField.SelectAllRequested += FileListViewModel_SelectAllRequested;
            boundField.InlineRenameRequested += FileListViewModel_InlineRenameRequested;
            boundField.ViewModeChanged += FileListViewModel_ViewModeChanged;
            boundField.PropertiesRequested += FileListViewModel_PropertiesRequested;
            boundField.SearchPanelOpened += FileListViewModel_SearchPanelOpened;
            // Only apply non-default view modes; Details is already set in XAML
            if (boundField.ViewMode != "Details")
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

    private void ApplyViewMode(ListView listView, string mode)
    {
        switch (mode)
        {
            case "Details":
                listView.View = CreateDetailsGridView(listView == RightPaneListView);
                listView.ItemTemplate = null;
                listView.ItemsPanel = null;
                break;
            case "List":
                listView.View = null;
                listView.ItemTemplate = (DataTemplate)FindResource("ListViewItemTemplate");
                listView.ItemsPanel = null;
                break;
            case "Tiles":
                listView.View = null;
                listView.ItemTemplate = (DataTemplate)FindResource("TileViewItemTemplate");
                listView.ItemsPanel = (ItemsPanelTemplate)FindResource("TileItemsPanelTemplate");
                break;
        }
    }

    private GridView CreateDetailsGridView(bool compact)
    {
        var gridView = new GridView();

        var nameColumn = new GridViewColumn { Header = "Name", Width = compact ? 250 : 350 };
        nameColumn.CellTemplate = (DataTemplate)FindResource("DetailsNameCellTemplate");
        gridView.Columns.Add(nameColumn);

        gridView.Columns.Add(new GridViewColumn
        {
            Header = "Size",
            Width = compact ? 80 : 100,
            DisplayMemberBinding = new System.Windows.Data.Binding("DisplaySize")
        });
        gridView.Columns.Add(new GridViewColumn
        {
            Header = "Type",
            Width = compact ? 100 : 120,
            DisplayMemberBinding = new System.Windows.Data.Binding("TypeDescription")
        });
        gridView.Columns.Add(new GridViewColumn
        {
            Header = "Date Modified",
            Width = compact ? 140 : 160,
            DisplayMemberBinding = new System.Windows.Data.Binding("DateModified")
            {
                StringFormat = "{0:yyyy-MM-dd HH:mm}"
            }
        });

        return gridView;
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

    private void RightPane_Drop(object sender, DragEventArgs e)
    {
        HandleDrop(e, ViewModel.RightPaneTab?.FileList);
    }

    private void HandleDrop(DragEventArgs e, FileListViewModel? fileList)
    {
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
