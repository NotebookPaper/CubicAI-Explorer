using System.Windows;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CubicAIExplorer.Models;

namespace CubicAIExplorer.Services;

public sealed partial class FileOperationQueueService : ObservableObject, IFileOperationQueueService
{
    private const int MaxRecentOperations = 8;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly object _stateLock = new();
    private readonly ObservableCollection<FileOperationQueueHistoryEntry> _recentOperations = [];
    private int _pendingCount;
    private CancellationTokenSource? _currentCancellationTokenSource;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _canCancel;

    [ObservableProperty]
    private bool _hasRecentActivity;

    [ObservableProperty]
    private string _currentOperationText = string.Empty;

    [ObservableProperty]
    private int _currentOperationCompletedSteps;

    [ObservableProperty]
    private int _currentOperationTotalSteps;

    [ObservableProperty]
    private string _currentOperationDetailText = string.Empty;

    [ObservableProperty]
    private string _lastCompletedOperationText = string.Empty;

    [ObservableProperty]
    private string _lastCompletedStatusText = string.Empty;

    public ReadOnlyObservableCollection<FileOperationQueueHistoryEntry> RecentOperations { get; }

    public FileOperationQueueService()
    {
        RecentOperations = new ReadOnlyObservableCollection<FileOperationQueueHistoryEntry>(_recentOperations);
    }

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
            var progressText = CurrentOperationProgressText;
            if (IsBusy && PendingCount > 0)
                return string.IsNullOrWhiteSpace(progressText)
                    ? $"{CurrentOperationText} ({PendingCount} queued)"
                    : $"{CurrentOperationText} {progressText} ({PendingCount} queued)";
            if (IsBusy)
                return string.IsNullOrWhiteSpace(progressText)
                    ? CurrentOperationText
                    : $"{CurrentOperationText} {progressText}";
            if (PendingCount > 0)
                return $"{PendingCount} queued";
            if (HasRecentActivity)
                return LastCompletedStatusText;
            return string.Empty;
        }
    }

    public double CurrentOperationProgressFraction
        => CurrentOperationTotalSteps <= 0
            ? 0
            : Math.Clamp((double)CurrentOperationCompletedSteps / CurrentOperationTotalSteps, 0, 1);

    public string CurrentOperationProgressText
        => CurrentOperationTotalSteps <= 0
            ? string.Empty
            : $"({Math.Min(CurrentOperationCompletedSteps, CurrentOperationTotalSteps)}/{CurrentOperationTotalSteps})";

    public async Task<T> EnqueueAsync<T>(string description, Func<T> operation)
        => await EnqueueAsync(description, _ => operation()).ConfigureAwait(false);

    public async Task<T> EnqueueAsync<T>(string description, Func<IFileOperationContext, T> operation)
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
            using var cancellationTokenSource = new CancellationTokenSource();
            SetBusyState(description, cancellationTokenSource, isBusy: true);
            var context = new FileOperationContext(this, cancellationTokenSource.Token);
            var result = await Task.Run(() => operation(context), cancellationTokenSource.Token).ConfigureAwait(false);
            SetCompletionState(
                description,
                $"{description} completed.",
                detailText: CurrentOperationDetailText,
                status: FileOperationQueueHistoryStatus.Succeeded);
            return result;
        }
        catch (OperationCanceledException)
        {
            SetCompletionState(
                description,
                $"{description} canceled.",
                detailText: CurrentOperationDetailText,
                status: FileOperationQueueHistoryStatus.Canceled);
            throw;
        }
        catch (Exception ex)
        {
            SetCompletionState(
                description,
                $"{description} failed: {ex.Message}",
                detailText: BuildFailureDetail(ex),
                status: FileOperationQueueHistoryStatus.Failed);
            throw;
        }
        finally
        {
            SetBusyState(string.Empty, cancellationTokenSource: null, isBusy: false);
            _semaphore.Release();
        }
    }

    public void CancelCurrent()
    {
        lock (_stateLock)
        {
            _currentCancellationTokenSource?.Cancel();
        }
    }

    partial void OnIsBusyChanged(bool value) => RaisePropertyChanged(nameof(StatusText));
    partial void OnCanCancelChanged(bool value) => RaisePropertyChanged(nameof(StatusText));
    partial void OnHasRecentActivityChanged(bool value) => RaisePropertyChanged(nameof(StatusText));
    partial void OnCurrentOperationTextChanged(string value) => RaisePropertyChanged(nameof(StatusText));
    partial void OnCurrentOperationCompletedStepsChanged(int value)
    {
        RaisePropertyChanged(nameof(CurrentOperationProgressFraction));
        RaisePropertyChanged(nameof(CurrentOperationProgressText));
        RaisePropertyChanged(nameof(StatusText));
    }
    partial void OnCurrentOperationTotalStepsChanged(int value)
    {
        RaisePropertyChanged(nameof(CurrentOperationProgressFraction));
        RaisePropertyChanged(nameof(CurrentOperationProgressText));
        RaisePropertyChanged(nameof(StatusText));
    }
    partial void OnCurrentOperationDetailTextChanged(string value) => RaisePropertyChanged(nameof(StatusText));
    partial void OnLastCompletedStatusTextChanged(string value) => RaisePropertyChanged(nameof(StatusText));

    private void SetBusyState(string description, CancellationTokenSource? cancellationTokenSource, bool isBusy)
    {
        RunOnUiThread(() =>
        {
            lock (_stateLock)
            {
                _currentCancellationTokenSource = isBusy ? cancellationTokenSource : null;
            }

            CurrentOperationText = description;
            CurrentOperationCompletedSteps = 0;
            CurrentOperationTotalSteps = 0;
            CurrentOperationDetailText = string.Empty;
            CanCancel = isBusy && cancellationTokenSource != null;
            IsBusy = isBusy;
        });
    }

    private void RaisePropertyChanged(string propertyName)
    {
        RunOnUiThread(() => OnPropertyChanged(propertyName));
    }

    private void SetCompletionState(
        string description,
        string statusText,
        string? detailText,
        FileOperationQueueHistoryStatus status)
    {
        RunOnUiThread(() =>
        {
            LastCompletedOperationText = description;
            LastCompletedStatusText = statusText;
            HasRecentActivity = true;
            AddRecentOperation(description, statusText, detailText, status);
        });
    }

    private void AddRecentOperation(
        string description,
        string statusText,
        string? detailText,
        FileOperationQueueHistoryStatus status)
    {
        _recentOperations.Insert(0, new FileOperationQueueHistoryEntry
        {
            OperationText = description,
            SummaryText = statusText,
            DetailText = detailText?.Trim() ?? string.Empty,
            Status = status,
            CompletedAtLocal = DateTime.Now
        });

        while (_recentOperations.Count > MaxRecentOperations)
            _recentOperations.RemoveAt(_recentOperations.Count - 1);

        OnPropertyChanged(nameof(RecentOperations));
    }

    private static string BuildFailureDetail(Exception ex)
    {
        var parts = new List<string>();
        Exception? current = ex;

        while (current != null)
        {
            if (!string.IsNullOrWhiteSpace(current.Message))
                parts.Add(current.Message.Trim());
            current = current.InnerException;
        }

        return string.Join(Environment.NewLine, parts.Distinct(StringComparer.Ordinal));
    }

    private void ReportProgress(int completedSteps, int totalSteps, string? detailText)
    {
        RunOnUiThread(() =>
        {
            CurrentOperationCompletedSteps = Math.Max(0, completedSteps);
            CurrentOperationTotalSteps = Math.Max(0, totalSteps);
            CurrentOperationDetailText = detailText ?? string.Empty;
        });
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

    private sealed class FileOperationContext : IFileOperationContext
    {
        private readonly FileOperationQueueService _owner;

        public FileOperationContext(FileOperationQueueService owner, CancellationToken cancellationToken)
        {
            _owner = owner;
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; }

        public void ReportProgress(int completedSteps, int totalSteps, string? detailText = null)
            => _owner.ReportProgress(completedSteps, totalSteps, detailText);
    }
}
