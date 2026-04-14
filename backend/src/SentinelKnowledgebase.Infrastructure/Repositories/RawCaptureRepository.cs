using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public class RawCaptureRepository : IRawCaptureRepository
{
    private readonly ApplicationDbContext _context;
    
    public RawCaptureRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public Task<RawCapture> AddAsync(RawCapture rawCapture)
    {
        _context.RawCaptures.Add(rawCapture);
        return Task.FromResult(rawCapture);
    }
    
    public async Task<RawCapture?> GetByIdAsync(Guid id)
    {
        return await _context.RawCaptures
            .Include(r => r.Tags)
            .Include(r => r.LabelAssignments)
                .ThenInclude(a => a.LabelCategory)
            .Include(r => r.LabelAssignments)
                .ThenInclude(a => a.LabelValue)
            .Include(r => r.ProcessedInsight)
                .ThenInclude(p => p.Tags)
            .Include(r => r.ProcessedInsight)
                .ThenInclude(p => p.LabelAssignments)
                    .ThenInclude(a => a.LabelCategory)
            .Include(r => r.ProcessedInsight)
                .ThenInclude(p => p.LabelAssignments)
                    .ThenInclude(a => a.LabelValue)
            .Include(r => r.ProcessedInsight)
                .ThenInclude(p => p.ClusterMembership)
                    .ThenInclude(membership => membership.InsightCluster)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<RawCapture?> GetByIdAsync(Guid id, Guid ownerUserId)
    {
        return await _context.RawCaptures
            .Include(r => r.Tags)
            .Include(r => r.LabelAssignments)
                .ThenInclude(a => a.LabelCategory)
            .Include(r => r.LabelAssignments)
                .ThenInclude(a => a.LabelValue)
            .Include(r => r.ProcessedInsight)
                .ThenInclude(p => p.Tags)
            .Include(r => r.ProcessedInsight)
                .ThenInclude(p => p.LabelAssignments)
                    .ThenInclude(a => a.LabelCategory)
            .Include(r => r.ProcessedInsight)
                .ThenInclude(p => p.LabelAssignments)
                    .ThenInclude(a => a.LabelValue)
            .Include(r => r.ProcessedInsight)
                .ThenInclude(p => p.ClusterMembership)
                    .ThenInclude(membership => membership.InsightCluster)
            .FirstOrDefaultAsync(r => r.Id == id && r.OwnerUserId == ownerUserId);
    }

    public async Task<IReadOnlyList<RawCapture>> GetByIdsAsync(Guid ownerUserId, IReadOnlyCollection<Guid> ids)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await _context.RawCaptures
            .Where(capture => capture.OwnerUserId == ownerUserId && ids.Contains(capture.Id))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<RawCapture>> GetByIdsWithGraphAsync(Guid ownerUserId, IReadOnlyCollection<Guid> ids)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await _context.RawCaptures
            .Include(capture => capture.Tags)
            .Include(capture => capture.LabelAssignments)
                .ThenInclude(assignment => assignment.LabelCategory)
            .Include(capture => capture.LabelAssignments)
                .ThenInclude(assignment => assignment.LabelValue)
            .Include(capture => capture.ProcessedInsight)
                .ThenInclude(insight => insight!.Tags)
            .Include(capture => capture.ProcessedInsight)
                .ThenInclude(insight => insight!.LabelAssignments)
                    .ThenInclude(assignment => assignment.LabelCategory)
            .Include(capture => capture.ProcessedInsight)
                .ThenInclude(insight => insight!.LabelAssignments)
                    .ThenInclude(assignment => assignment.LabelValue)
            .Where(capture => capture.OwnerUserId == ownerUserId && ids.Contains(capture.Id))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<CaptureMetadataRecord>> GetCompletedTweetsWithMetadataAsync(Guid ownerUserId, int take)
    {
        var normalizedTake = Math.Clamp(take, 1, 5000);

        return await _context.RawCaptures
            .AsNoTracking()
            .Where(capture =>
                capture.OwnerUserId == ownerUserId &&
                capture.ContentType == ContentType.Tweet &&
                capture.Status == CaptureStatus.Completed &&
                capture.Metadata != null)
            .OrderByDescending(capture => capture.CreatedAt)
            .Take(normalizedTake)
            .Select(capture => new CaptureMetadataRecord
            {
                Id = capture.Id,
                SourceUrl = capture.SourceUrl,
                RawContent = capture.RawContent,
                Metadata = capture.Metadata,
                CreatedAt = capture.CreatedAt,
                ProcessedAt = capture.ProcessedAt
            })
            .ToListAsync();
    }

    public async Task<CaptureSearchQueryResult> SearchCapturesAsync(Guid ownerUserId, CaptureSearchQueryOptions options)
    {
        var page = options.Page > 0 ? options.Page : 1;
        var pageSize = Math.Clamp(options.PageSize > 0 ? options.PageSize : 20, 1, 100);
        var maxResultSetSize = Math.Clamp(options.MaxResultSetSize, 1, 5000);
        var threshold = Math.Clamp(options.Threshold, 0, 1);
        var hasQuery = !string.IsNullOrWhiteSpace(options.Query);

        var filteredQuery = ApplyCaptureSearchFilters(
            _context.RawCaptures
                .AsNoTracking()
                .Where(capture => capture.OwnerUserId == ownerUserId),
            options);

        IOrderedQueryable<CaptureSearchRecord> orderedQuery;
        if (hasQuery)
        {
            var normalizedQuery = options.Query!.Trim();
            var pattern = $"%{normalizedQuery}%";
            var queryVector = options.QueryEmbedding is { Length: > 0 } ? new Vector(options.QueryEmbedding) : null;

            filteredQuery = filteredQuery.Where(capture =>
                EF.Functions.ILike(capture.RawContent, pattern) ||
                EF.Functions.ILike(capture.SourceUrl, pattern) ||
                (capture.Metadata != null && EF.Functions.ILike(capture.Metadata, pattern)) ||
                (queryVector != null &&
                 capture.Status == CaptureStatus.Completed &&
                 capture.ProcessedInsight != null &&
                 capture.ProcessedInsight.EmbeddingVector != null &&
                 1 - capture.ProcessedInsight.EmbeddingVector.Vector.CosineDistance(queryVector) >= threshold));

            var rankedQuery = filteredQuery
                .Select(capture => new CaptureSearchRecord
                {
                    CaptureId = capture.Id,
                    SourceUrl = capture.SourceUrl,
                    RawContent = capture.RawContent,
                    Metadata = capture.Metadata,
                    ContentType = capture.ContentType,
                    Status = capture.Status,
                    CreatedAt = capture.CreatedAt,
                    Similarity = queryVector != null &&
                                 capture.Status == CaptureStatus.Completed &&
                                 capture.ProcessedInsight != null &&
                                 capture.ProcessedInsight.EmbeddingVector != null
                        ? 1 - capture.ProcessedInsight.EmbeddingVector.Vector.CosineDistance(queryVector)
                        : null,
                    MatchedByText = EF.Functions.ILike(capture.RawContent, pattern) ||
                                    EF.Functions.ILike(capture.SourceUrl, pattern) ||
                                    (capture.Metadata != null && EF.Functions.ILike(capture.Metadata, pattern)),
                    MatchedBySemantic = queryVector != null &&
                                        capture.Status == CaptureStatus.Completed &&
                                        capture.ProcessedInsight != null &&
                                        capture.ProcessedInsight.EmbeddingVector != null &&
                                        1 - capture.ProcessedInsight.EmbeddingVector.Vector.CosineDistance(queryVector) >= threshold
                });

            orderedQuery = rankedQuery
                .OrderByDescending(record => record.Similarity ?? -1d)
                .ThenByDescending(record => record.MatchedByText)
                .ThenByDescending(record => record.CreatedAt)
                .ThenBy(record => record.CaptureId);
        }
        else
        {
            var unrankedQuery = filteredQuery
                .Select(capture => new CaptureSearchRecord
                {
                    CaptureId = capture.Id,
                    SourceUrl = capture.SourceUrl,
                    RawContent = capture.RawContent,
                    Metadata = capture.Metadata,
                    ContentType = capture.ContentType,
                    Status = capture.Status,
                    CreatedAt = capture.CreatedAt,
                    Similarity = null,
                    MatchedByText = false,
                    MatchedBySemantic = false
                });

            orderedQuery = unrankedQuery
                .OrderByDescending(record => record.CreatedAt)
                .ThenBy(record => record.CaptureId);
        }

        var totalCount = await orderedQuery.CountAsync();
        var captureIds = await orderedQuery
            .Select(record => record.CaptureId)
            .Take(maxResultSetSize)
            .ToListAsync();
        var items = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new CaptureSearchQueryResult
        {
            CaptureIds = captureIds,
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<RawCapture>> GetFailedAsync(Guid ownerUserId, ContentType? contentType = null)
    {
        IQueryable<RawCapture> query = _context.RawCaptures
            .Where(capture => capture.OwnerUserId == ownerUserId && capture.Status == CaptureStatus.Failed);

        if (contentType.HasValue)
        {
            query = query.Where(capture => capture.ContentType == contentType.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<CaptureListQueryResult> GetPagedListAsync(Guid ownerUserId, CaptureListQueryOptions options)
    {
        IQueryable<RawCapture> query = _context.RawCaptures
            .AsNoTracking()
            .Where(capture => capture.OwnerUserId == ownerUserId);

        if (options.ContentType.HasValue)
        {
            query = query.Where(capture => capture.ContentType == options.ContentType.Value);
        }

        if (options.Status.HasValue)
        {
            query = query.Where(capture => capture.Status == options.Status.Value);
        }

        query = ApplySort(query, options);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((options.Page - 1) * options.PageSize)
            .Take(options.PageSize)
            .Select(capture => new CaptureListRecord
            {
                Id = capture.Id,
                SourceUrl = capture.SourceUrl,
                ContentType = capture.ContentType,
                Status = capture.Status,
                CreatedAt = capture.CreatedAt,
                ProcessedAt = capture.ProcessedAt,
                Metadata = capture.Metadata
            })
            .ToListAsync();

        return new CaptureListQueryResult
        {
            Items = items,
            TotalCount = totalCount
        };
    }
    
    public async Task<IEnumerable<RawCapture>> GetAllAsync(Guid ownerUserId)
    {
        return await _context.RawCaptures
            .Include(r => r.Tags)
            .Include(r => r.LabelAssignments)
                .ThenInclude(a => a.LabelCategory)
            .Include(r => r.LabelAssignments)
                .ThenInclude(a => a.LabelValue)
            .Include(r => r.ProcessedInsight)
                .ThenInclude(p => p.Tags)
            .Include(r => r.ProcessedInsight)
                .ThenInclude(p => p.LabelAssignments)
                    .ThenInclude(a => a.LabelCategory)
            .Include(r => r.ProcessedInsight)
                .ThenInclude(p => p.LabelAssignments)
                    .ThenInclude(a => a.LabelValue)
            .Include(r => r.ProcessedInsight)
                .ThenInclude(p => p.ClusterMembership)
                    .ThenInclude(membership => membership.InsightCluster)
            .Where(r => r.OwnerUserId == ownerUserId)
            .ToListAsync();
    }

    public async Task<IEnumerable<RawCapture>> GetRecentAsync(Guid ownerUserId, int take)
    {
        return await _context.RawCaptures
            .Include(r => r.Tags)
            .Include(r => r.LabelAssignments)
                .ThenInclude(a => a.LabelCategory)
            .Include(r => r.LabelAssignments)
                .ThenInclude(a => a.LabelValue)
            .Include(r => r.ProcessedInsight)
                .ThenInclude(p => p.ClusterMembership)
                    .ThenInclude(membership => membership.InsightCluster)
            .Where(r => r.OwnerUserId == ownerUserId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<RawCapture>> GetRecentGlobalAsync(int take)
    {
        return await _context.RawCaptures
            .Include(r => r.Tags)
            .Include(r => r.LabelAssignments)
                .ThenInclude(a => a.LabelCategory)
            .Include(r => r.LabelAssignments)
                .ThenInclude(a => a.LabelValue)
            .Include(r => r.ProcessedInsight)
                .ThenInclude(p => p.ClusterMembership)
                    .ThenInclude(membership => membership.InsightCluster)
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IReadOnlyDictionary<CaptureStatus, int>> GetStatusCountsAsync()
    {
        return await _context.RawCaptures
            .GroupBy(capture => capture.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(item => item.Status, item => item.Count);
    }

    public async Task<IReadOnlyList<Guid>> GetPendingIdsAsync()
    {
        return await _context.RawCaptures
            .Where(capture => capture.Status == CaptureStatus.Pending)
            .Select(capture => capture.Id)
            .ToListAsync();
    }

    public Task<int> CountAsync(Guid ownerUserId)
    {
        return _context.RawCaptures.CountAsync(r => r.OwnerUserId == ownerUserId);
    }
    
    public Task UpdateAsync(RawCapture rawCapture)
    {
        _context.RawCaptures.Update(rawCapture);
        return Task.CompletedTask;
    }
    
    public async Task DeleteAsync(Guid id, Guid ownerUserId)
    {
        var rawCapture = await _context.RawCaptures
            .FirstOrDefaultAsync(r => r.Id == id && r.OwnerUserId == ownerUserId);
        if (rawCapture != null)
        {
            _context.RawCaptures.Remove(rawCapture);
        }
    }

    public async Task<int> DeleteByIdsAsync(Guid ownerUserId, IReadOnlyCollection<Guid> ids)
    {
        if (ids.Count == 0)
        {
            return 0;
        }

        return await _context.RawCaptures
            .Where(capture => capture.OwnerUserId == ownerUserId && ids.Contains(capture.Id))
            .ExecuteDeleteAsync();
    }

    private static IQueryable<RawCapture> ApplySort(IQueryable<RawCapture> query, CaptureListQueryOptions options)
    {
        var descending = string.Equals(options.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return options.SortField switch
        {
            "contentType" => descending
                ? query.OrderByDescending(capture => capture.ContentType).ThenByDescending(capture => capture.CreatedAt)
                : query.OrderBy(capture => capture.ContentType).ThenByDescending(capture => capture.CreatedAt),
            "status" => descending
                ? query.OrderByDescending(capture => capture.Status).ThenByDescending(capture => capture.CreatedAt)
                : query.OrderBy(capture => capture.Status).ThenByDescending(capture => capture.CreatedAt),
            "sourceUrl" => descending
                ? query.OrderByDescending(capture => capture.SourceUrl).ThenByDescending(capture => capture.CreatedAt)
                : query.OrderBy(capture => capture.SourceUrl).ThenByDescending(capture => capture.CreatedAt),
            _ => descending
                ? query.OrderByDescending(capture => capture.CreatedAt)
                : query.OrderBy(capture => capture.CreatedAt)
        };
    }

    private static IQueryable<RawCapture> ApplyCaptureSearchFilters(
        IQueryable<RawCapture> query,
        CaptureSearchQueryOptions options)
    {
        if (options.ContentType.HasValue)
        {
            query = query.Where(capture => capture.ContentType == options.ContentType.Value);
        }

        if (options.Status.HasValue)
        {
            query = query.Where(capture => capture.Status == options.Status.Value);
        }

        if (options.DateFrom.HasValue)
        {
            query = query.Where(capture => capture.CreatedAt >= options.DateFrom.Value);
        }

        if (options.DateTo.HasValue)
        {
            query = query.Where(capture => capture.CreatedAt <= options.DateTo.Value);
        }

        var normalizedTags = options.Tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedTags.Count > 0)
        {
            query = query.Where(capture => capture.Tags.Any(tag => normalizedTags.Contains(tag.Name)));

            if (options.MatchAllTags)
            {
                query = query.Where(capture =>
                    capture.Tags
                        .Where(tag => normalizedTags.Contains(tag.Name))
                        .Select(tag => tag.Name)
                        .Distinct()
                        .Count() == normalizedTags.Count);
            }
        }

        var normalizedLabels = options.Labels
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

            query = query.Where(capture => capture.LabelAssignments.Any(assignment =>
                normalizedKeys.Contains(assignment.LabelCategory.Name + "\u001f" + assignment.LabelValue.Value)));

            if (options.MatchAllLabels)
            {
                query = query.Where(capture =>
                    capture.LabelAssignments
                        .Where(assignment => normalizedKeys.Contains(assignment.LabelCategory.Name + "\u001f" + assignment.LabelValue.Value))
                        .Select(assignment => assignment.LabelCategory.Name + "\u001f" + assignment.LabelValue.Value)
                        .Distinct()
                        .Count() == normalizedKeys.Count);
            }
        }

        return query;
    }
}
