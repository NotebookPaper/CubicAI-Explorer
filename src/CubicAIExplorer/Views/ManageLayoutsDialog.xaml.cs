using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CubicAIExplorer.Models;

namespace CubicAIExplorer.Views;

public partial class ManageLayoutsDialog : Window
{
    private readonly Action<string> _applyLayout;
    private readonly Func<string, bool> _deleteLayout;

    public ObservableCollection<WindowLayout> Layouts { get; } = [];

    public ManageLayoutsDialog(
        IEnumerable<WindowLayout> layouts,
        string? selectedLayoutName,
        Action<string> applyLayout,
        Func<string, bool> deleteLayout)
    {
        _applyLayout = applyLayout;
        _deleteLayout = deleteLayout;

        InitializeComponent();

        foreach (var layout in layouts)
            Layouts.Add(layout);

        LayoutsListBox.ItemsSource = Layouts;
        if (!string.IsNullOrWhiteSpace(selectedLayoutName))
        {
            LayoutsListBox.SelectedItem = Layouts.FirstOrDefault(layout =>
                string.Equals(layout.Name, selectedLayoutName, StringComparison.OrdinalIgnoreCase));
        }

        if (LayoutsListBox.SelectedItem == null && Layouts.Count > 0)
            LayoutsListBox.SelectedIndex = 0;

        LayoutsListBox.SelectionChanged += (_, _) => UpdateState();
        Loaded += (_, _) =>
        {
            UpdateState();
            LayoutsListBox.Focus();
        };
    }

    private void ApplySelectedLayout()
    {
        if (LayoutsListBox.SelectedItem is not WindowLayout layout)
            return;

        _applyLayout(layout.Name);
    }

    private void UpdateState()
    {
        var hasSelection = LayoutsListBox.SelectedItem is WindowLayout;
        ApplyButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection;
        EmptyStateText.Visibility = Layouts.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        ApplySelectedLayout();
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (LayoutsListBox.SelectedItem is not WindowLayout layout)
            return;

        if (!_deleteLayout(layout.Name))
            return;

        Layouts.Remove(layout);
        if (Layouts.Count > 0)
            LayoutsListBox.SelectedIndex = Math.Min(LayoutsListBox.SelectedIndex, Layouts.Count - 1);

        UpdateState();
    }

    private void LayoutsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ApplySelectedLayout();
    }
}
