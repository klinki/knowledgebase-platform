using Pgvector;
using SentinelKnowledgebase.Application.DTOs;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Application.Services;

public class SearchService : ISearchService
{
    private readonly IProcessedInsightRepository _insightRepository;
    private readonly ITagRepository _tagRepository;

    public SearchService(IProcessedInsightRepository insightRepository, ITagRepository tagRepository)
    {
        _insightRepository = insightRepository;
        _tagRepository = tagRepository;
    }

    public async Task<IEnumerable<SemanticSearchResponse>> SemanticSearchAsync(SemanticSearchRequest request, CancellationToken cancellationToken = default)
    {
        // For now, return empty results - embedding generation will be implemented
        // when OpenAI integration is added
        var embedding = new float[1536]; // Placeholder - should come from OpenAI
        
        var results = await _insightRepository.SearchByEmbeddingAsync(embedding, request.Limit, cancellationToken);
        
        return results.Select(r => new SemanticSearchResponse
        {
            Id = r.Id,
            Title = r.Title,
            Summary = r.Summary,
            SourceUrl = r.RawCapture.SourceUrl,
            Similarity = 1.0 // Placeholder - should calculate actual cosine similarity
        });
    }

    public async Task<IEnumerable<TagSearchResponse>> SearchByTagAsync(TagSearchRequest request, CancellationToken cancellationToken = default)
    {
        var captures = await _tagRepository.GetCapturesByTagAsync(request.Tag, cancellationToken);
        
        return captures
            .Where(c => c.Status == CaptureStatus.Completed && c.ProcessedInsight != null)
            .Select(c => new TagSearchResponse
            {
                Id = c.ProcessedInsight!.Id,
                Title = c.ProcessedInsight.Title,
                Summary = c.ProcessedInsight.Summary,
                SourceUrl = c.SourceUrl,
                CreatedAt = c.CreatedAt,
                Tags = c.Tags.Select(t => t.Name).ToList()
            });
    }
}
