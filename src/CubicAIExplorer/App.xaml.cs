using System.Windows;
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

        var fileSystemService = new FileSystemService();
        var clipboardService = new ClipboardService();
        var mainViewModel = new MainViewModel(fileSystemService, clipboardService);
        mainViewModel.NewTabCommand.Execute(null);

        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };

        _singleInstance.ArgumentsReceived += (_, args) =>
        {
            Dispatcher.Invoke(() =>
            {
                mainWindow.Activate();
                if (args.Length > 0 && fileSystemService.DirectoryExists(args[0]))
                {
                    mainViewModel.NavigateToPath(args[0]);
                }
            });
        };

        // Navigate to command-line path if provided
        if (e.Args.Length > 0 && fileSystemService.DirectoryExists(e.Args[0]))
        {
            mainViewModel.NavigateToPath(e.Args[0]);
        }

        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
