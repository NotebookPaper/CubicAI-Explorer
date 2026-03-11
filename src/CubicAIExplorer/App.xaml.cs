using System.IO;
using System.Text.Json;
using System.Windows;
using CubicAIExplorer.Converters;
using CubicAIExplorer.Models;
using CubicAIExplorer.Services;
using CubicAIExplorer.ViewModels;

namespace CubicAIExplorer;

public partial class App : Application
{
    private SingleInstanceService? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new SingleInstanceService();
        if (!_singleInstance.TryAcquire())
        {
            SingleInstanceService.SendArgumentsToRunningInstance(e.Args);
            Shutdown();
            return;
        }

        var settingsService = new SettingsService();
        var settings = settingsService.Load();

        var fileSystemService = new FileSystemService();
        var clipboardService = new ClipboardService();
        var fileOperationQueueService = new FileOperationQueueService();
        var bookmarkService = new BookmarkService();
        var shellIconService = new ShellIconService();
        ShellIconConverter.IconService = shellIconService;
        var mainViewModel = new MainViewModel(
            fileSystemService,
            clipboardService,
            settingsService,
            settings,
            fileOperationQueueService,
            bookmarkService);

        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };

        _singleInstance.ArgumentsReceived += (_, args) =>
        {
            Dispatcher.Invoke(() =>
            {
                mainWindow.Activate();
                var resolvedPath = args.Length > 0 ? fileSystemService.ResolveDirectoryPath(args[0]) : null;
                if (!string.IsNullOrWhiteSpace(resolvedPath))
                {
                    mainViewModel.NavigateToPath(resolvedPath);
                }
            });
        };

        // Navigate to command-line path if provided
        var startupPath = e.Args.Length > 0 ? fileSystemService.ResolveDirectoryPath(e.Args[0]) : null;
        if (!string.IsNullOrWhiteSpace(startupPath))
        {
            mainViewModel.NavigateToPath(startupPath);
        }

        // Apply startup preferences
        if (settings.StartInDualPane)
            mainViewModel.ToggleDualPaneCommand.Execute(null);
        if (settings.StartWithPreview)
            mainViewModel.TogglePreviewCommand.Execute(null);

        RestoreWindowBounds(mainWindow);
        mainWindow.Closing += (_, _) => SaveWindowBounds(mainWindow);
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    private static string GetSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "CubicAIExplorer", "window.json");
    }

    private static void SaveWindowBounds(Window window)
    {
        try
        {
            var bounds = new WindowBounds
            {
                Left = window.Left,
                Top = window.Top,
                Width = window.Width,
                Height = window.Height,
                IsMaximized = window.WindowState == WindowState.Maximized
            };

            var path = GetSettingsPath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, JsonSerializer.Serialize(bounds));
        }
        catch
        {
            // Persistence failures should not prevent shutdown.
        }
    }

    private static void RestoreWindowBounds(Window window)
    {
        try
        {
            var path = GetSettingsPath();
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            var bounds = JsonSerializer.Deserialize<WindowBounds>(json);
            if (bounds == null) return;

            // Validate bounds are within screen area
            var screenWidth = SystemParameters.VirtualScreenWidth;
            var screenHeight = SystemParameters.VirtualScreenHeight;

            if (bounds.Left >= 0 && bounds.Left < screenWidth
                && bounds.Top >= 0 && bounds.Top < screenHeight
                && bounds.Width > 100 && bounds.Height > 100)
            {
                window.Left = bounds.Left;
                window.Top = bounds.Top;
                window.Width = Math.Min(bounds.Width, screenWidth);
                window.Height = Math.Min(bounds.Height, screenHeight);
                window.WindowStartupLocation = WindowStartupLocation.Manual;

                if (bounds.IsMaximized)
                    window.WindowState = WindowState.Maximized;
            }
        }
        catch
        {
            // Fall back to default position.
        }
    }

    private sealed class WindowBounds
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsMaximized { get; set; }
    }
}
