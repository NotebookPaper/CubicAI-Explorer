using System.Windows;
using CubicAIExplorer.Models;

namespace CubicAIExplorer.Views;

public partial class QueueFailureDetailsDialog : Window
{
    public QueueFailureDetailsDialog(QueueHistoryEntry entry)
    {
        InitializeComponent();
        OperationTextBlock.Text = $"{entry.Description} — {entry.ItemFailures.Count} failure(s):";
        FailureListView.ItemsSource = entry.ItemFailures;
    }
}
