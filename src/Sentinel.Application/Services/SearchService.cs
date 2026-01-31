using Sentinel.Application.Dtos;
using Sentinel.Application.Interfaces;
using Sentinel.Application.Mapping;

namespace Sentinel.Application.Services;

public sealed class SearchService : ISearchService
{
    private readonly IProcessedInsightRepository _processedInsightRepository;
    private readonly IEmbeddingService _embeddingService;

    public SearchService(IProcessedInsightRepository processedInsightRepository, IEmbeddingService embeddingService)
    {
        _processedInsightRepository = processedInsightRepository;
        _embeddingService = embeddingService;
    }

    public async Task<IReadOnlyCollection<ProcessedInsightDto>> SearchSemanticAsync(
        SemanticSearchRequest request,
        CancellationToken cancellationToken)
    {
        var embedding = await _embeddingService.GenerateAsync(request.Query, cancellationToken);
        var insights = await _processedInsightRepository.SearchSemanticAsync(embedding, request.Limit, cancellationToken);

        return insights.Select(ProcessedInsightMapper.ToDto).ToArray();
    }

    public async Task<IReadOnlyCollection<ProcessedInsightDto>> SearchByTagsAsync(
        TagSearchRequest request,
        CancellationToken cancellationToken)
    {
        var insights = await _processedInsightRepository.SearchByTagsAsync(request.Tags, request.MatchAll, request.Limit, cancellationToken);

        return insights.Select(ProcessedInsightMapper.ToDto).ToArray();
    }
}
