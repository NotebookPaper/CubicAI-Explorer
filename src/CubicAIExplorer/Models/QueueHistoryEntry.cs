namespace CubicAIExplorer.Models;

public enum QueueHistoryStatus { Completed, Canceled, Failed }

public sealed record QueueItemFailure(string ItemName, string ErrorMessage);

public sealed record QueueHistoryEntry(
    string Description,
    QueueHistoryStatus Status,
    DateTime CompletedAt,
    IReadOnlyList<QueueItemFailure> ItemFailures)
{
    public string StatusIcon => Status switch
    {
        QueueHistoryStatus.Canceled => "⊘",
        QueueHistoryStatus.Failed => "✗",
        _ => ItemFailures.Count > 0 ? "⚠" : "✓"
    };

    public string TimeText => CompletedAt.ToString("HH:mm:ss");
    public bool HasFailures => ItemFailures.Count > 0;
    public string FailureSummary => $"{ItemFailures.Count} failed";
}
