using System.Linq;
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
            NewFileTemplatesPath = settings.NewFileTemplatesPath,
            StartInDualPane = settings.StartInDualPane,
            StartWithPreview = settings.StartWithPreview,
            UseShellContextMenu = settings.UseShellContextMenu,
            ExternalTools = settings.ExternalTools
                .Select(static tool => new ExternalTool
                {
                    Name = tool.Name,
                    ToolPath = tool.ToolPath,
                    Arguments = tool.Arguments
                })
                .ToList()
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

    private void BrowseTemplatesFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select new-file template folder"
        };

        if (!string.IsNullOrWhiteSpace(Settings.NewFileTemplatesPath)
            && System.IO.Directory.Exists(Settings.NewFileTemplatesPath))
        {
            dialog.InitialDirectory = Settings.NewFileTemplatesPath;
        }

        if (dialog.ShowDialog(this) == true)
        {
            Settings.NewFileTemplatesPath = dialog.FolderName;
            TemplatesFolderBox.Text = dialog.FolderName;
        }
    }

    private void AddExternalTool_Click(object sender, RoutedEventArgs e)
    {
        var tool = new ExternalTool
        {
            Name = "New Tool"
        };

        Settings.ExternalTools.Add(tool);
        ExternalToolsGrid.SelectedItem = tool;
        ExternalToolsGrid.ScrollIntoView(tool);
    }

    private void BrowseExternalTool_Click(object sender, RoutedEventArgs e)
    {
        if (ExternalToolsGrid.SelectedItem is not ExternalTool tool)
        {
            MessageBox.Show(this, "Select a tool row first.", "External Tools", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select external tool",
            Filter = "Programs (*.exe)|*.exe|All files (*.*)|*.*"
        };

        if (!string.IsNullOrWhiteSpace(tool.ToolPath))
        {
            var directory = System.IO.Path.GetDirectoryName(tool.ToolPath);
            if (!string.IsNullOrWhiteSpace(directory) && System.IO.Directory.Exists(directory))
            {
                dialog.InitialDirectory = directory;
            }
        }

        if (dialog.ShowDialog(this) == true)
        {
            tool.ToolPath = dialog.FileName;
            if (string.IsNullOrWhiteSpace(tool.Name))
                tool.Name = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);

            ExternalToolsGrid.Items.Refresh();
        }
    }

    private void RemoveExternalTool_Click(object sender, RoutedEventArgs e)
    {
        if (ExternalToolsGrid.SelectedItem is not ExternalTool tool)
            return;

        Settings.ExternalTools.Remove(tool);
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        // Read ComboBox selection
        if (ViewModeCombo.SelectedItem is System.Windows.Controls.ComboBoxItem selected
            && selected.Content is string mode)
        {
            Settings.DefaultViewMode = mode;
        }

        for (var i = Settings.ExternalTools.Count - 1; i >= 0; i--)
        {
            var tool = Settings.ExternalTools[i];
            tool.Name = tool.Name?.Trim() ?? string.Empty;
            tool.ToolPath = tool.ToolPath?.Trim() ?? string.Empty;
            tool.Arguments = tool.Arguments?.Trim() ?? string.Empty;

            var hasName = !string.IsNullOrWhiteSpace(tool.Name);
            var hasPath = !string.IsNullOrWhiteSpace(tool.ToolPath);
            if (!hasName && !hasPath && string.IsNullOrWhiteSpace(tool.Arguments))
            {
                Settings.ExternalTools.RemoveAt(i);
                continue;
            }

            if (!hasName || !hasPath)
            {
                MessageBox.Show(
                    this,
                    "Each external tool needs both a name and a program path.",
                    "External Tools",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
