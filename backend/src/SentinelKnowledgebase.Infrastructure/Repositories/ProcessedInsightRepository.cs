using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public class ProcessedInsightRepository : IProcessedInsightRepository
{
    private const string SortDirectionDesc = "desc";
    private const string SortFieldRelevance = "relevance";
    private const string SortFieldProcessedAt = "processedAt";
    private const string SortFieldTitle = "title";
    private const string SortFieldSourceUrl = "sourceUrl";
    private readonly ApplicationDbContext _context;
    
    public ProcessedInsightRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public Task<ProcessedInsight> AddAsync(ProcessedInsight processedInsight)
    {
        _context.ProcessedInsights.Add(processedInsight);
        return Task.FromResult(processedInsight);
    }
    
    public async Task<ProcessedInsight?> GetByIdAsync(Guid id)
    {
        return await _context.ProcessedInsights
            .Include(p => p.Tags)
            .Include(p => p.LabelAssignments)
                .ThenInclude(a => a.LabelCategory)
            .Include(p => p.LabelAssignments)
                .ThenInclude(a => a.LabelValue)
            .Include(p => p.EmbeddingVector)
            .Include(p => p.ClusterMembership)
                .ThenInclude(membership => membership.InsightCluster)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
    
    public async Task<IEnumerable<ProcessedInsight>> GetAllAsync()
    {
        return await _context.ProcessedInsights
            .Include(p => p.Tags)
            .Include(p => p.LabelAssignments)
                .ThenInclude(a => a.LabelCategory)
            .Include(p => p.LabelAssignments)
                .ThenInclude(a => a.LabelValue)
            .Include(p => p.EmbeddingVector)
            .Include(p => p.ClusterMembership)
                .ThenInclude(membership => membership.InsightCluster)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ProcessedInsightEmbeddingRecord>> GetEmbeddingRecordsAsync(Guid ownerUserId)
    {
        return await _context.ProcessedInsights
            .AsNoTracking()
            .Where(insight => insight.OwnerUserId == ownerUserId && insight.EmbeddingVector != null)
            .Select(insight => new ProcessedInsightEmbeddingRecord
            {
                Id = insight.Id,
                OwnerUserId = insight.OwnerUserId,
                Title = insight.Title,
                Summary = insight.Summary,
                SourceUrl = insight.RawCapture.SourceUrl,
                Embedding = insight.EmbeddingVector!.Vector.ToArray()
            })
            .ToListAsync();
    }

    public async Task<SearchQueryResult> SearchAsync(
        Guid ownerUserId,
        float[]? queryEmbedding,
        double threshold,
        int page,
        int pageSize,
        IReadOnlyCollection<string> tags,
        bool matchAllTags,
        IReadOnlyCollection<LabelRecord> labels,
        bool matchAllLabels,
        Guid? topicClusterId,
        string sortField,
        string sortDirection)
    {
        var skip = (page - 1) * pageSize;
        var baseQuery = _context.ProcessedInsights
            .AsNoTracking()
            .Where(p => p.OwnerUserId == ownerUserId);
        if (topicClusterId.HasValue)
        {
            baseQuery = baseQuery.Where(p =>
                p.ClusterMembership != null &&
                p.ClusterMembership.InsightClusterId == topicClusterId.Value &&
                p.ClusterMembership.InsightCluster.OwnerUserId == ownerUserId);
        }

        var query = ApplyTagAndLabelFilters(
            baseQuery,
            tags,
            matchAllTags,
            labels,
            matchAllLabels);

        if (queryEmbedding is not null)
        {
            var queryVector = new Vector(queryEmbedding);
            var filteredQuery = query
                .Where(p => p.EmbeddingVector != null)
                .Where(p => 1 - p.EmbeddingVector!.Vector.CosineDistance(queryVector) >= threshold);
            var totalCount = await filteredQuery.CountAsync();

            var projectedQuery = filteredQuery
                .Select(p => new SearchRecord
                {
                    Id = p.Id,
                    CaptureId = p.RawCaptureId,
                    Title = p.Title,
                    Summary = p.Summary,
                    SourceUrl = p.RawCapture.SourceUrl,
                    ProcessedAt = p.ProcessedAt,
                    Similarity = 1 - p.EmbeddingVector!.Vector.CosineDistance(queryVector),
                    Tags = p.Tags.Select(t => t.Name).ToList(),
                    Labels = p.LabelAssignments
                        .OrderBy(a => a.LabelCategory.Name)
                        .ThenBy(a => a.LabelValue.Value)
                        .Select(a => new LabelRecord
                        {
                            Category = a.LabelCategory.Name,
                            Value = a.LabelValue.Value
                    })
                    .ToList()
                });

            var orderedQuery = ApplySearchSort(projectedQuery, sortField, sortDirection, hasQuery: true);
            var items = await orderedQuery
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return new SearchQueryResult
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        var totalStructuredCount = await query.CountAsync();
        var projectedStructuredQuery = query
            .Select(p => new SearchRecord
            {
                Id = p.Id,
                CaptureId = p.RawCaptureId,
                Title = p.Title,
                Summary = p.Summary,
                SourceUrl = p.RawCapture.SourceUrl,
                ProcessedAt = p.ProcessedAt,
                Similarity = null,
                Tags = p.Tags.Select(t => t.Name).ToList(),
                Labels = p.LabelAssignments
                    .OrderBy(a => a.LabelCategory.Name)
                    .ThenBy(a => a.LabelValue.Value)
                    .Select(a => new LabelRecord
                    {
                        Category = a.LabelCategory.Name,
                        Value = a.LabelValue.Value
                    })
                    .ToList()
            });

        var orderedStructuredQuery = ApplySearchSort(projectedStructuredQuery, sortField, sortDirection, hasQuery: false);
        var structuredItems = await orderedStructuredQuery
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        return new SearchQueryResult
        {
            Items = structuredItems,
            TotalCount = totalStructuredCount
        };
    }

    public async Task<IEnumerable<SemanticSearchRecord>> SemanticSearchAsync(Guid ownerUserId, float[] queryEmbedding, int topK, double threshold)
    {
        var queryVector = new Vector(queryEmbedding);

        return await _context.ProcessedInsights
            .AsNoTracking()
            .Where(p => p.OwnerUserId == ownerUserId && p.EmbeddingVector != null)
            .Select(p => new SemanticSearchRecord
            {
                Id = p.Id,
                Title = p.Title,
                Summary = p.Summary,
                SourceUrl = p.RawCapture.SourceUrl,
                Similarity = 1 - p.EmbeddingVector!.Vector.CosineDistance(queryVector),
                Tags = p.Tags.Select(t => t.Name).ToList(),
                Labels = p.LabelAssignments
                    .OrderBy(a => a.LabelCategory.Name)
                    .ThenBy(a => a.LabelValue.Value)
                    .Select(a => new LabelRecord
                    {
                        Category = a.LabelCategory.Name,
                        Value = a.LabelValue.Value
                    })
                    .ToList()
            })
            .Where(r => r.Similarity >= threshold)
            .OrderByDescending(r => r.Similarity)
            .Take(topK)
            .ToListAsync();
    }

    public async Task<IEnumerable<TagSearchRecord>> SearchByTagsAsync(Guid ownerUserId, IReadOnlyCollection<string> tags, bool matchAll)
    {
        var normalizedTags = tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct()
            .ToList();

        var query = _context.ProcessedInsights
            .AsNoTracking()
            .Where(p => p.OwnerUserId == ownerUserId)
            .Where(p => p.Tags.Any(t => normalizedTags.Contains(t.Name)));

        if (matchAll)
        {
            query = query.Where(p =>
                p.Tags
                    .Where(t => normalizedTags.Contains(t.Name))
                    .Select(t => t.Name)
                    .Distinct()
                    .Count() == normalizedTags.Count);
        }

        return await query
            .Select(p => new TagSearchRecord
            {
                Id = p.Id,
                Title = p.Title,
                Summary = p.Summary,
                SourceUrl = p.RawCapture.SourceUrl,
                Tags = p.Tags.Select(t => t.Name).ToList(),
                Labels = p.LabelAssignments
                    .OrderBy(a => a.LabelCategory.Name)
                    .ThenBy(a => a.LabelValue.Value)
                    .Select(a => new LabelRecord
                    {
                        Category = a.LabelCategory.Name,
                        Value = a.LabelValue.Value
                    })
                    .ToList(),
                ProcessedAt = p.ProcessedAt
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<LabelSearchRecord>> SearchByLabelsAsync(
        Guid ownerUserId,
        IReadOnlyCollection<LabelRecord> labels,
        bool matchAll)
    {
        var normalizedLabels = labels
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

        if (normalizedLabels.Count == 0)
        {
            return [];
        }

        var normalizedKeys = normalizedLabels
            .Select(label => $"{label.Category}\u001f{label.Value}")
            .ToList();

        var query = _context.ProcessedInsights
            .AsNoTracking()
            .Where(p => p.OwnerUserId == ownerUserId)
            .Where(p => p.LabelAssignments.Any(a =>
                normalizedKeys.Contains(a.LabelCategory.Name + "\u001f" + a.LabelValue.Value)));

        if (matchAll)
        {
            query = query.Where(p =>
                p.LabelAssignments
                    .Where(a => normalizedKeys.Contains(a.LabelCategory.Name + "\u001f" + a.LabelValue.Value))
                    .Select(a => a.LabelCategory.Name + "\u001f" + a.LabelValue.Value)
                    .Distinct()
                    .Count() == normalizedKeys.Count);
        }

        return await query
            .Select(p => new LabelSearchRecord
            {
                Id = p.Id,
                Title = p.Title,
                Summary = p.Summary,
                SourceUrl = p.RawCapture.SourceUrl,
                Tags = p.Tags.Select(t => t.Name).ToList(),
                Labels = p.LabelAssignments
                    .OrderBy(a => a.LabelCategory.Name)
                    .ThenBy(a => a.LabelValue.Value)
                    .Select(a => new LabelRecord
                    {
                        Category = a.LabelCategory.Name,
                        Value = a.LabelValue.Value
                    })
                    .ToList(),
                ProcessedAt = p.ProcessedAt
            })
            .ToListAsync();
    }
    
    public Task UpdateAsync(ProcessedInsight processedInsight)
    {
        _context.ProcessedInsights.Update(processedInsight);
        return Task.CompletedTask;
    }
    
    public async Task DeleteAsync(Guid id)
    {
        var processedInsight = await _context.ProcessedInsights.FindAsync(id);
        if (processedInsight != null)
        {
            _context.ProcessedInsights.Remove(processedInsight);
        }
    }

    private static IQueryable<ProcessedInsight> ApplyTagAndLabelFilters(
        IQueryable<ProcessedInsight> query,
        IReadOnlyCollection<string> tags,
        bool matchAllTags,
        IReadOnlyCollection<LabelRecord> labels,
        bool matchAllLabels)
    {
        var normalizedTags = tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedTags.Count > 0)
        {
            query = query.Where(p => p.Tags.Any(tag => normalizedTags.Contains(tag.Name)));

            if (matchAllTags)
            {
                query = query.Where(p =>
                    p.Tags
                        .Where(tag => normalizedTags.Contains(tag.Name))
                        .Select(tag => tag.Name)
                        .Distinct()
                        .Count() == normalizedTags.Count);
            }
        }

        var normalizedLabels = labels
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

        if (normalizedLabels.Count > 0)
        {
            var normalizedKeys = normalizedLabels
                .Select(label => $"{label.Category}\u001f{label.Value}")
                .ToList();

            query = query.Where(p => p.LabelAssignments.Any(assignment =>
                normalizedKeys.Contains(assignment.LabelCategory.Name + "\u001f" + assignment.LabelValue.Value)));

            if (matchAllLabels)
            {
                query = query.Where(p =>
                    p.LabelAssignments
                        .Where(assignment => normalizedKeys.Contains(assignment.LabelCategory.Name + "\u001f" + assignment.LabelValue.Value))
                        .Select(assignment => assignment.LabelCategory.Name + "\u001f" + assignment.LabelValue.Value)
                        .Distinct()
                        .Count() == normalizedKeys.Count);
            }
        }

        return query;
    }

    private static IOrderedQueryable<SearchRecord> ApplySearchSort(
        IQueryable<SearchRecord> query,
        string sortField,
        string sortDirection,
        bool hasQuery)
    {
        var descending = string.Equals(sortDirection, SortDirectionDesc, StringComparison.OrdinalIgnoreCase);
        var normalizedSortField = sortField.Trim();

        if (!hasQuery && string.Equals(normalizedSortField, SortFieldRelevance, StringComparison.OrdinalIgnoreCase))
        {
            normalizedSortField = SortFieldProcessedAt;
        }

        return normalizedSortField switch
        {
            _ when string.Equals(normalizedSortField, SortFieldRelevance, StringComparison.OrdinalIgnoreCase) && descending
                => query.OrderByDescending(result => result.Similarity ?? -1d).ThenBy(result => result.Id),
            _ when string.Equals(normalizedSortField, SortFieldRelevance, StringComparison.OrdinalIgnoreCase)
                => query.OrderBy(result => result.Similarity ?? -1d).ThenBy(result => result.Id),
            _ when string.Equals(normalizedSortField, SortFieldTitle, StringComparison.OrdinalIgnoreCase) && descending
                => query.OrderByDescending(result => result.Title).ThenBy(result => result.Id),
            _ when string.Equals(normalizedSortField, SortFieldTitle, StringComparison.OrdinalIgnoreCase)
                => query.OrderBy(result => result.Title).ThenBy(result => result.Id),
            _ when string.Equals(normalizedSortField, SortFieldSourceUrl, StringComparison.OrdinalIgnoreCase) && descending
                => query.OrderByDescending(result => result.SourceUrl).ThenBy(result => result.Id),
            _ when string.Equals(normalizedSortField, SortFieldSourceUrl, StringComparison.OrdinalIgnoreCase)
                => query.OrderBy(result => result.SourceUrl).ThenBy(result => result.Id),
            _ when descending
                => query.OrderByDescending(result => result.ProcessedAt).ThenBy(result => result.Id),
            _ => query.OrderBy(result => result.ProcessedAt).ThenBy(result => result.Id)
        };
    }
}
