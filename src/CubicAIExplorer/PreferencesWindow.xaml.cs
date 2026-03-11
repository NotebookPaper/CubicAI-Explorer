using System.Windows;
using CubicAIExplorer.Models;

namespace CubicAIExplorer;

public partial class PreferencesWindow : Window
{
    public UserSettings Settings { get; }

    public PreferencesWindow(UserSettings settings)
    {
        InitializeComponent();

        // Clone so cancel doesn't mutate the original
        Settings = new UserSettings
        {
            DefaultViewMode = settings.DefaultViewMode,
            ShowHiddenFiles = settings.ShowHiddenFiles,
            StartupFolder = settings.StartupFolder,
            StartInDualPane = settings.StartInDualPane,
            StartWithPreview = settings.StartWithPreview,
            UseShellContextMenu = settings.UseShellContextMenu
        };

        DataContext = Settings;

        // Set ComboBox selection to match the setting
        foreach (System.Windows.Controls.ComboBoxItem item in ViewModeCombo.Items)
        {
            if (item.Content is string s && s == Settings.DefaultViewMode)
            {
                ViewModeCombo.SelectedItem = item;
                break;
            }
        }
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select startup folder"
        };

        if (!string.IsNullOrWhiteSpace(Settings.StartupFolder)
            && System.IO.Directory.Exists(Settings.StartupFolder))
        {
            dialog.InitialDirectory = Settings.StartupFolder;
        }

        if (dialog.ShowDialog(this) == true)
        {
            Settings.StartupFolder = dialog.FolderName;
            StartupFolderBox.Text = dialog.FolderName;
        }
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        // Read ComboBox selection
        if (ViewModeCombo.SelectedItem is System.Windows.Controls.ComboBoxItem selected
            && selected.Content is string mode)
        {
            Settings.DefaultViewMode = mode;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
