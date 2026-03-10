using System.ComponentModel;

namespace CubicAIExplorer.Services;

public interface IFileOperationQueueService : INotifyPropertyChanged
{
    bool IsBusy { get; }
    int PendingCount { get; }
    string CurrentOperationText { get; }
    string StatusText { get; }
    Task<T> EnqueueAsync<T>(string description, Func<T> operation);
}
