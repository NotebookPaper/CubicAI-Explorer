using System.ComponentModel;

namespace CubicAIExplorer.Services;

public interface IFileOperationContext
{
    CancellationToken CancellationToken { get; }
    void ReportProgress(int completedSteps, int totalSteps, string? detailText = null);
}

public interface IFileOperationQueueService : INotifyPropertyChanged
{
    bool IsBusy { get; }
    bool CanCancel { get; }
    int PendingCount { get; }
    bool HasRecentActivity { get; }
    string CurrentOperationText { get; }
    int CurrentOperationCompletedSteps { get; }
    int CurrentOperationTotalSteps { get; }
    double CurrentOperationProgressFraction { get; }
    string CurrentOperationProgressText { get; }
    string CurrentOperationDetailText { get; }
    string StatusText { get; }
    string LastCompletedOperationText { get; }
    string LastCompletedStatusText { get; }
    Task<T> EnqueueAsync<T>(string description, Func<T> operation);
    Task<T> EnqueueAsync<T>(string description, Func<IFileOperationContext, T> operation);
    void CancelCurrent();
}
