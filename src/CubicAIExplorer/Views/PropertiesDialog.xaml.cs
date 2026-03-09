using System.IO;
using System.Windows;
using CubicAIExplorer.Models;

namespace CubicAIExplorer.Views;

public partial class PropertiesDialog : Window
{
    public PropertiesDialog(FileSystemItem item)
    {
        InitializeComponent();

        FileNameText.Text = item.Name;
        TypeText.Text = item.TypeDescription;
        LocationText.Text = Path.GetDirectoryName(item.FullPath) ?? item.FullPath;
        CreatedText.Text = item.DateCreated.ToString("yyyy-MM-dd HH:mm:ss");
        ModifiedText.Text = item.DateModified.ToString("yyyy-MM-dd HH:mm:ss");
        ReadOnlyCheck.IsChecked = item.IsReadOnly;
        HiddenCheck.IsChecked = item.IsHidden;

        if (item.ItemType == FileSystemItemType.File)
        {
            SizeText.Text = FormatDetailedSize(item.Size);
        }
        else if (item.ItemType == FileSystemItemType.Directory)
        {
            SizeText.Text = "";
            try
            {
                var dirInfo = new DirectoryInfo(item.FullPath);
                var fileCount = dirInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Count();
                var dirCount = dirInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).Count();
                ContainsLabel.Visibility = Visibility.Visible;
                ContainsText.Visibility = Visibility.Visible;
                ContainsText.Text = $"{fileCount} files, {dirCount} folders";
            }
            catch
            {
                // Access denied — leave blank
            }
        }

        Title = $"{item.Name} Properties";
    }

    private void OK_Click(object sender, RoutedEventArgs e) => Close();

    private static string FormatDetailedSize(long bytes)
    {
        var formatted = bytes switch
        {
            < 1024 => $"{bytes} bytes",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
            _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
        };
        return $"{formatted} ({bytes:N0} bytes)";
    }
}
