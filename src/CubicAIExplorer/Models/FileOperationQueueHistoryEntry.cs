namespace CubicAIExplorer.Models;

public enum FileOperationQueueHistoryStatus
{
    Succeeded,
    Failed,
    Canceled
}

public sealed class FileOperationQueueHistoryEntry
{
    public required string OperationText { get; init; }
    public required string SummaryText { get; init; }
    public string DetailText { get; init; } = string.Empty;
    public FileOperationQueueHistoryStatus Status { get; init; }
    public DateTime CompletedAtLocal { get; init; }
}
