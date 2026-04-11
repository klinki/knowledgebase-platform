using Microsoft.EntityFrameworkCore;
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
}
