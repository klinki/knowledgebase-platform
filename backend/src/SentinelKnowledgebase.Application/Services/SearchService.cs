using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SentinelKnowledgebase.Application.DTOs.Search;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
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
        
        var processedInsights = await _unitOfWork.ProcessedInsights.GetAllAsync();
        var results = new List<SemanticSearchResultDto>();
        
        foreach (var insight in processedInsights)
        {
            var embedding = await _unitOfWork.EmbeddingVectors.GetByProcessedInsightIdAsync(insight.Id);
            if (embedding != null)
            {
                var similarity = CalculateCosineSimilarity(queryEmbedding, embedding.Vector.ToArray());
                if (similarity >= request.Threshold)
                {
                    results.Add(new SemanticSearchResultDto
                    {
                        Id = insight.Id,
                        Title = insight.Title,
                        Summary = insight.Summary,
                        SourceUrl = insight.RawCapture.SourceUrl,
                        Similarity = similarity,
                        Tags = insight.Tags.Select(t => t.Name).ToList()
                    });
                }
            }
        }
        
        return results
            .OrderByDescending(r => r.Similarity)
            .Take(request.TopK);
    }
    
    public async Task<IEnumerable<TagSearchResultDto>> SearchByTagsAsync(TagSearchRequestDto request)
    {
        var allInsights = await _unitOfWork.ProcessedInsights.GetAllAsync();
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
                    SourceUrl = insight.RawCapture.SourceUrl,
                    Tags = insightTagNames.ToList(),
                    ProcessedAt = insight.ProcessedAt
                });
            }
        }
        
        return results;
    }
    
    private double CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
            return 0;
        
        double dotProduct = 0;
        double normA = 0;
        double normB = 0;
        
        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }
        
        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
