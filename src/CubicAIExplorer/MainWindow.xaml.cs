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

    public MainWindow()
    {
        InitializeComponent();
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
}
