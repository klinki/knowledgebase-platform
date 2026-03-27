using SentinelKnowledgebase.Application.DTOs.Search;
using SentinelKnowledgebase.Application.DTOs.Labels;
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
    
    public async Task<IEnumerable<SemanticSearchResultDto>> SemanticSearchAsync(Guid ownerUserId, SemanticSearchRequestDto request)
    {
        var queryEmbedding = await _contentProcessor.GenerateEmbeddingAsync(request.Query);

        var results = await _unitOfWork.ProcessedInsights
            .SemanticSearchAsync(ownerUserId, queryEmbedding, request.TopK, request.Threshold);

        return results.Select(r => new SemanticSearchResultDto
        {
            Id = r.Id,
            Title = r.Title,
                Summary = r.Summary,
                SourceUrl = r.SourceUrl,
                Similarity = r.Similarity,
                Tags = r.Tags,
                Labels = MapLabels(r.Labels)
            });
    }
    
    public async Task<IEnumerable<TagSearchResultDto>> SearchByTagsAsync(Guid ownerUserId, TagSearchRequestDto request)
    {
        var results = await _unitOfWork.ProcessedInsights
            .SearchByTagsAsync(ownerUserId, request.Tags, request.MatchAll);

        return results.Select(r => new TagSearchResultDto
        {
            Id = r.Id,
                Title = r.Title,
                Summary = r.Summary,
                SourceUrl = r.SourceUrl,
                Tags = r.Tags,
                Labels = MapLabels(r.Labels),
                ProcessedAt = r.ProcessedAt
            });
    }

    public async Task<IEnumerable<LabelSearchResultDto>> SearchByLabelsAsync(Guid ownerUserId, LabelSearchRequestDto request)
    {
        var results = await _unitOfWork.ProcessedInsights
            .SearchByLabelsAsync(
                ownerUserId,
                request.Labels.Select(label => new LabelRecord
                {
                    Category = label.Category,
                    Value = label.Value
                }).ToList(),
                request.MatchAll);

        return results.Select(r => new LabelSearchResultDto
        {
            Id = r.Id,
            Title = r.Title,
            Summary = r.Summary,
            SourceUrl = r.SourceUrl,
            Tags = r.Tags,
            Labels = MapLabels(r.Labels),
            ProcessedAt = r.ProcessedAt
        });
    }

    private static List<LabelAssignmentDto> MapLabels(IEnumerable<LabelRecord> labels)
    {
        return labels
            .Select(label => new LabelAssignmentDto
            {
                Category = label.Category,
                Value = label.Value
            })
            .ToList();
    }
}
