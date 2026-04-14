using SentinelKnowledgebase.Application.DTOs.Labels;
using SentinelKnowledgebase.Application.DTOs.Search;
using SentinelKnowledgebase.Domain.Enums;

namespace SentinelKnowledgebase.Application.Services.Interfaces;

public interface ICaptureBulkActionService
{
    Task<CaptureBulkQueryResult> SearchCapturesAsync(
        Guid ownerUserId,
        CaptureSearchCriteria criteria,
        int maxResultSetSize,
        int previewSize);

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

public class CaptureSearchCriteria
{
    public string? Query { get; set; }
    public List<string> Tags { get; set; } = new();
    public string TagMatchMode { get; set; } = SearchMatchModes.Any;
    public List<LabelAssignmentDto> Labels { get; set; } = new();
    public string LabelMatchMode { get; set; } = SearchMatchModes.All;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public double Threshold { get; set; } = 0.3;
    public ContentType? ContentType { get; set; }
    public CaptureStatus? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
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
    public string? ContentType { get; set; }
    public string? Status { get; set; }
    public double? Similarity { get; set; }
    public string? MatchReason { get; set; }
    public string? PreviewText { get; set; }
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
