using SentinelKnowledgebase.Application.DTOs;

namespace SentinelKnowledgebase.Application.Services;

public interface ICaptureService
{
    Task<CaptureResponse> CreateCaptureAsync(CaptureRequest request, CancellationToken cancellationToken = default);
    Task<InsightResponse?> GetInsightAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ISearchService
{
    Task<IEnumerable<SemanticSearchResponse>> SemanticSearchAsync(SemanticSearchRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<TagSearchResponse>> SearchByTagAsync(TagSearchRequest request, CancellationToken cancellationToken = default);
}

public interface IProcessingService
{
    Task ProcessPendingCapturesAsync(CancellationToken cancellationToken = default);
    Task<InsightResponse?> ProcessCaptureAsync(Guid captureId, CancellationToken cancellationToken = default);
}
