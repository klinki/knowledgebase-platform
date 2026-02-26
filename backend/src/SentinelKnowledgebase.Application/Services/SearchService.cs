using SentinelKnowledgebase.Application.DTOs.Search;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Application.Services;

public class SearchService : ISearchService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContentProcessor _contentProcessor;
    
    public SearchService(IUnitOfWork unitOfWork, IContentProcessor contentProcessor)
    {
        _unitOfWork = unitOfWork;
        _contentProcessor = contentProcessor;
    }
    
    public async Task<IEnumerable<SemanticSearchResultDto>> SemanticSearchAsync(SemanticSearchRequestDto request)
    {
        var queryEmbedding = await _contentProcessor.GenerateEmbeddingAsync(request.Query);

        var results = await _unitOfWork.ProcessedInsights
            .SemanticSearchAsync(queryEmbedding, request.TopK, request.Threshold);

        return results.Select(r => new SemanticSearchResultDto
        {
            Id = r.Id,
            Title = r.Title,
            Summary = r.Summary,
            SourceUrl = r.SourceUrl,
            Similarity = r.Similarity,
            Tags = r.Tags
        });
    }
    
    public async Task<IEnumerable<TagSearchResultDto>> SearchByTagsAsync(TagSearchRequestDto request)
    {
        var results = await _unitOfWork.ProcessedInsights
            .SearchByTagsAsync(request.Tags, request.MatchAll);

        return results.Select(r => new TagSearchResultDto
        {
            Id = r.Id,
            Title = r.Title,
            Summary = r.Summary,
            SourceUrl = r.SourceUrl,
            Tags = r.Tags,
            ProcessedAt = r.ProcessedAt
        });
    }
}
