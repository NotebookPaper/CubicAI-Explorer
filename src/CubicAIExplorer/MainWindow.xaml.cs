using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
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

    private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.ActiveTab?.FileList.SelectedItem is { } item)
        {
            ViewModel.ActiveTab.FileList.OpenItemCommand.Execute(item);
        }
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
        }

        _boundFileListViewModel = fileListViewModel;
        if (_boundFileListViewModel != null)
        {
            _boundFileListViewModel.SelectAllRequested += FileListViewModel_SelectAllRequested;
        }
    }

    private void FileListViewModel_SelectAllRequested(object? sender, EventArgs e)
    {
        FileListView.SelectAll();
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
}
