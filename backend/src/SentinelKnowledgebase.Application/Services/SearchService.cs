using SentinelKnowledgebase.Application.DTOs.Search;
using SentinelKnowledgebase.Application.DTOs.Labels;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Application.Services;

public class SearchService : ISearchService
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const double DefaultThreshold = 0.6;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContentProcessor _contentProcessor;
    
    public SearchService(IUnitOfWork unitOfWork, IContentProcessor contentProcessor)
    {
        _unitOfWork = unitOfWork;
        _contentProcessor = contentProcessor;
    }

    public async Task<SearchResultPageDto> SearchAsync(Guid ownerUserId, SearchRequestDto request)
    {
        var normalizedQuery = NormalizeQuery(request.Query);
        var normalizedTags = NormalizeTags(request.Tags);
        var normalizedLabels = NormalizeLabels(request.Labels);
        var topicClusterId = request.TopicClusterId;
        var hasQuery = normalizedQuery is not null;

        if (normalizedQuery is null && topicClusterId is null && normalizedTags.Count == 0 && normalizedLabels.Count == 0)
        {
            throw new ArgumentException("At least one search criterion is required.", nameof(request));
        }

        var page = request.Page > 0 ? request.Page : DefaultPage;
        var pageSize = request.PageSize > 0 ? request.PageSize : DefaultPageSize;
        var threshold = request.Threshold >= 0 ? request.Threshold : DefaultThreshold;
        var (sortField, sortDirection) = NormalizeSort(request.SortField, request.SortDirection, hasQuery);
        float[]? queryEmbedding = null;

        if (normalizedQuery is not null)
        {
            queryEmbedding = await _contentProcessor.GenerateEmbeddingAsync(normalizedQuery);
        }

        var results = await _unitOfWork.ProcessedInsights.SearchAsync(
            ownerUserId,
            queryEmbedding,
            threshold,
            page,
            pageSize,
            normalizedTags,
            SearchMatchModes.IsAll(request.TagMatchMode),
            normalizedLabels,
            SearchMatchModes.IsAll(request.LabelMatchMode),
            topicClusterId,
            sortField,
            sortDirection);

        return new SearchResultPageDto
        {
            Items = results.Items.Select(result => new SearchResultDto
            {
                Id = result.Id,
                CaptureId = result.CaptureId,
                Title = result.Title,
                Summary = result.Summary,
                SourceUrl = result.SourceUrl,
                ProcessedAt = result.ProcessedAt,
                Tags = result.Tags,
                Labels = MapLabels(result.Labels),
                Similarity = result.Similarity
            }).ToList(),
            TotalCount = results.TotalCount,
            Page = page,
            PageSize = pageSize
        };
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

    private static string? NormalizeQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        return query.Trim();
    }

    private static IReadOnlyList<string> NormalizeTags(IEnumerable<string>? tags)
    {
        return (tags ?? [])
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<LabelRecord> NormalizeLabels(IEnumerable<LabelAssignmentDto>? labels)
    {
        return (labels ?? [])
            .Where(label =>
                !string.IsNullOrWhiteSpace(label.Category) &&
                !string.IsNullOrWhiteSpace(label.Value))
            .Select(label => new LabelRecord
            {
                Category = label.Category.Trim(),
                Value = label.Value.Trim()
            })
            .GroupBy(label => $"{label.Category}\u001f{label.Value}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static (string sortField, string sortDirection) NormalizeSort(
        string? requestedSortField,
        string? requestedSortDirection,
        bool hasQuery)
    {
        var sortField = NormalizeSortField(requestedSortField);
        if (sortField == null)
        {
            sortField = hasQuery
                ? ProcessedInsightSearchSortFields.Relevance
                : ProcessedInsightSearchSortFields.ProcessedAt;
        }

        if (!hasQuery && string.Equals(sortField, ProcessedInsightSearchSortFields.Relevance, StringComparison.OrdinalIgnoreCase))
        {
            sortField = ProcessedInsightSearchSortFields.ProcessedAt;
            return (sortField, SearchSortDirections.Desc);
        }

        var sortDirection = SearchSortDirections.IsValid(requestedSortDirection)
            ? requestedSortDirection!.Trim().ToLowerInvariant()
            : SearchSortDirections.Desc;

        return (sortField, sortDirection);
    }

    private static string? NormalizeSortField(string? sortField)
    {
        if (!ProcessedInsightSearchSortFields.IsValid(sortField))
        {
            return null;
        }

        var normalized = sortField!.Trim();
        return normalized switch
        {
            _ when string.Equals(normalized, ProcessedInsightSearchSortFields.Relevance, StringComparison.OrdinalIgnoreCase) =>
                ProcessedInsightSearchSortFields.Relevance,
            _ when string.Equals(normalized, ProcessedInsightSearchSortFields.ProcessedAt, StringComparison.OrdinalIgnoreCase) =>
                ProcessedInsightSearchSortFields.ProcessedAt,
            _ when string.Equals(normalized, ProcessedInsightSearchSortFields.Title, StringComparison.OrdinalIgnoreCase) =>
                ProcessedInsightSearchSortFields.Title,
            _ => ProcessedInsightSearchSortFields.SourceUrl
        };
    }
}
