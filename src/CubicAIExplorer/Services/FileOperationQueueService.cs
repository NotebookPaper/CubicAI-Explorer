using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CubicAIExplorer.Services;

public sealed partial class FileOperationQueueService : ObservableObject, IFileOperationQueueService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly object _stateLock = new();
    private int _pendingCount;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _currentOperationText = string.Empty;

    public int PendingCount
    {
        get
        {
            lock (_stateLock)
            {
                return _pendingCount;
            }
        }
        private set
        {
            lock (_stateLock)
            {
                if (_pendingCount == value)
                    return;

                _pendingCount = value;
            }

            RaisePropertyChanged(nameof(PendingCount));
            RaisePropertyChanged(nameof(StatusText));
        }
    }

    public string StatusText
    {
        get
        {
            if (IsBusy && PendingCount > 0)
                return $"{CurrentOperationText} ({PendingCount} queued)";
            if (IsBusy)
                return CurrentOperationText;
            if (PendingCount > 0)
                return $"{PendingCount} queued";
            return string.Empty;
        }
    }

    public async Task<T> EnqueueAsync<T>(string description, Func<T> operation)
    {
        PendingCount += 1;
        try
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
        }
        finally
        {
            PendingCount -= 1;
        }

        try
        {
            SetBusyState(description, isBusy: true);
            return await Task.Run(operation).ConfigureAwait(false);
        }
        finally
        {
            SetBusyState(string.Empty, isBusy: false);
            _semaphore.Release();
        }
    }

    partial void OnIsBusyChanged(bool value) => RaisePropertyChanged(nameof(StatusText));
    partial void OnCurrentOperationTextChanged(string value) => RaisePropertyChanged(nameof(StatusText));

    private void SetBusyState(string description, bool isBusy)
    {
        RunOnUiThread(() =>
        {
            CurrentOperationText = description;
            IsBusy = isBusy;
        });
    }

    private void RaisePropertyChanged(string propertyName)
    {
        RunOnUiThread(() => OnPropertyChanged(propertyName));
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }
}
