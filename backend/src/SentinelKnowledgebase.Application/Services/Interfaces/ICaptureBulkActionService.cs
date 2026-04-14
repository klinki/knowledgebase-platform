using SentinelKnowledgebase.Application.DTOs.Labels;

namespace SentinelKnowledgebase.Application.Services.Interfaces;

public interface ICaptureBulkActionService
{
    Task<CaptureBulkQueryResult> FindDeletedTweetsFromUnavailableAccountsAsync(
        Guid ownerUserId,
        int maxResultSetSize,
        int previewSize);

    Task<CaptureBulkMutationResult> AddTagsAsync(
        Guid ownerUserId,
        IReadOnlyCollection<Guid> captureIds,
        IReadOnlyCollection<string> tags);

    Task<CaptureBulkMutationResult> RemoveTagsAsync(
        Guid ownerUserId,
        IReadOnlyCollection<Guid> captureIds,
        IReadOnlyCollection<string> tags);

    Task<CaptureBulkMutationResult> AddLabelsAsync(
        Guid ownerUserId,
        IReadOnlyCollection<Guid> captureIds,
        IReadOnlyCollection<LabelAssignmentDto> labels);

    Task<CaptureBulkMutationResult> RemoveLabelsAsync(
        Guid ownerUserId,
        IReadOnlyCollection<Guid> captureIds,
        IReadOnlyCollection<LabelAssignmentDto> labels);

    Task<int> DeleteCapturesAsync(Guid ownerUserId, IReadOnlyCollection<Guid> captureIds);
}

public class CaptureBulkQueryResult
{
    public List<Guid> CaptureIds { get; set; } = new();
    public List<CaptureBulkPreviewItem> PreviewItems { get; set; } = new();
    public int TotalCount { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class CaptureBulkPreviewItem
{
    public Guid CaptureId { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string? SkipCode { get; set; }
    public string? SkipReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CaptureBulkMutationResult
{
    public int RequestedCount { get; set; }
    public int MatchedCount { get; set; }
    public int MutatedCount { get; set; }
}
