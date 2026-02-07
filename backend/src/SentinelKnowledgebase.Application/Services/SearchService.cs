using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentinelKnowledgebase.Application.DTOs.Search;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Data;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Application.Services;

public class SearchService : ISearchService
{
    private readonly ApplicationDbContext _context;
    private readonly IContentProcessor _contentProcessor;
    private readonly ILogger<SearchService> _logger;

    public SearchService(
        ApplicationDbContext context,
        IContentProcessor contentProcessor,
        ILogger<SearchService> logger)
    {
        _context = context;
        _contentProcessor = contentProcessor;
        _logger = logger;
    }

    public async Task<IEnumerable<SemanticSearchResultDto>> SemanticSearchAsync(SemanticSearchRequestDto request)
    {
        var queryEmbedding = await _contentProcessor.GenerateEmbeddingAsync(request.Query);

        // Single query with Include to avoid N+1 problem
        var insightsWithEmbeddings = await _context.ProcessedInsights
            .Include(p => p.Tags)
            .Include(p => p.EmbeddingVector)
            .Include(p => p.RawCapture)
            .Where(p => p.EmbeddingVector != null)
            .ToListAsync();

        var results = new List<SemanticSearchResultDto>();

        foreach (var insight in insightsWithEmbeddings)
        {
            if (insight.EmbeddingVector == null) continue;

            var similarity = CalculateCosineSimilarity(queryEmbedding, insight.EmbeddingVector.Vector.ToArray());
            if (similarity >= request.Threshold)
            {
                results.Add(new SemanticSearchResultDto
                {
                    Id = insight.Id,
                    Title = insight.Title,
                    Summary = insight.Summary,
                    SourceUrl = insight.RawCapture?.SourceUrl ?? string.Empty,
                    Similarity = similarity,
                    Tags = insight.Tags.Select(t => t.Name).ToList()
                });
            }
        }

        _logger.LogDebug(
            "Semantic search for '{Query}' returned {Count} results above threshold {Threshold}",
            request.Query, results.Count, request.Threshold);

        return results
            .OrderByDescending(r => r.Similarity)
            .Take(request.TopK);
    }

    public async Task<IEnumerable<TagSearchResultDto>> SearchByTagsAsync(TagSearchRequestDto request)
    {
        // Single query with Include to avoid N+1 problem
        var allInsights = await _context.ProcessedInsights
            .Include(p => p.Tags)
            .Include(p => p.RawCapture)
            .ToListAsync();

        var results = new List<TagSearchResultDto>();

        foreach (var insight in allInsights)
        {
            var insightTagNames = insight.Tags.Select(t => t.Name).ToHashSet();
            var matches = request.Tags.Count(tag => insightTagNames.Contains(tag));

            bool include = request.MatchAll
                ? matches == request.Tags.Count
                : matches > 0;

            if (include)
            {
                results.Add(new TagSearchResultDto
                {
                    Id = insight.Id,
                    Title = insight.Title,
                    Summary = insight.Summary,
                    SourceUrl = insight.RawCapture?.SourceUrl ?? string.Empty,
                    Tags = insightTagNames.ToList(),
                    ProcessedAt = insight.ProcessedAt
                });
            }
        }

        _logger.LogDebug(
            "Tag search for tags [{Tags}] (MatchAll={MatchAll}) returned {Count} results",
            string.Join(", ", request.Tags), request.MatchAll, results.Count);

        return results;
    }

    private double CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
        {
            _logger.LogWarning(
                "Vector length mismatch: {LengthA} vs {LengthB}",
                vectorA.Length, vectorB.Length);
            return 0;
        }

        double dotProduct = 0;
        double normA = 0;
        double normB = 0;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }

        var denominator = Math.Sqrt(normA) * Math.Sqrt(normB);
        return denominator > 0 ? dotProduct / denominator : 0;
    }
}
