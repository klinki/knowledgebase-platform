using Microsoft.EntityFrameworkCore;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public class InsightClusterRepository : IInsightClusterRepository
{
    private readonly ApplicationDbContext _context;

    public InsightClusterRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<InsightCluster> AddAsync(InsightCluster cluster)
    {
        _context.InsightClusters.Add(cluster);
        return Task.FromResult(cluster);
    }

    public Task AddMembershipsAsync(IEnumerable<InsightClusterMembership> memberships)
    {
        _context.InsightClusterMemberships.AddRange(memberships);
        return Task.CompletedTask;
    }

    public async Task<InsightCluster?> GetByIdAsync(Guid ownerUserId, Guid clusterId)
    {
        return await _context.InsightClusters
            .AsNoTracking()
            .Include(cluster => cluster.Memberships.OrderBy(membership => membership.Rank))
                .ThenInclude(membership => membership.ProcessedInsight)
                    .ThenInclude(insight => insight.RawCapture)
            .Include(cluster => cluster.Memberships)
                .ThenInclude(membership => membership.ProcessedInsight)
                    .ThenInclude(insight => insight.Tags)
            .Include(cluster => cluster.Memberships)
                .ThenInclude(membership => membership.ProcessedInsight)
                    .ThenInclude(insight => insight.LabelAssignments)
                        .ThenInclude(assignment => assignment.LabelCategory)
            .Include(cluster => cluster.Memberships)
                .ThenInclude(membership => membership.ProcessedInsight)
                    .ThenInclude(insight => insight.LabelAssignments)
                        .ThenInclude(assignment => assignment.LabelValue)
            .FirstOrDefaultAsync(cluster => cluster.OwnerUserId == ownerUserId && cluster.Id == clusterId);
    }

    public async Task<IReadOnlyList<InsightCluster>> GetTopAsync(Guid ownerUserId, int take)
    {
        return await _context.InsightClusters
            .AsNoTracking()
            .Where(cluster => cluster.OwnerUserId == ownerUserId)
            .OrderByDescending(cluster => cluster.MemberCount)
            .ThenByDescending(cluster => cluster.UpdatedAt)
            .Include(cluster => cluster.Memberships.OrderBy(membership => membership.Rank).Take(3))
                .ThenInclude(membership => membership.ProcessedInsight)
                    .ThenInclude(insight => insight.RawCapture)
            .Take(take)
            .ToListAsync();
    }

    public async Task<TopicClusterQueryResult> GetPagedAsync(Guid ownerUserId, TopicClusterQueryOptions options)
    {
        var query = _context.InsightClusters
            .AsNoTracking()
            .Where(cluster => cluster.OwnerUserId == ownerUserId);

        var normalizedQuery = options.Query?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            query = query.Where(cluster =>
                cluster.Title.ToLower().Contains(normalizedQuery) ||
                (cluster.Description != null && cluster.Description.ToLower().Contains(normalizedQuery)) ||
                cluster.KeywordsJson.ToLower().Contains(normalizedQuery));
        }

        var totalCount = await query.CountAsync();
        var page = Math.Max(1, options.Page);
        var pageSize = Math.Clamp(options.PageSize, 1, 100);

        var sortedQuery = ApplySorting(query, options);
        var items = await sortedQuery
            .Include(cluster => cluster.Memberships.OrderBy(membership => membership.Rank).Take(3))
                .ThenInclude(membership => membership.ProcessedInsight)
                    .ThenInclude(insight => insight.RawCapture)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new TopicClusterQueryResult
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    private static IQueryable<InsightCluster> ApplySorting(IQueryable<InsightCluster> query, TopicClusterQueryOptions options)
    {
        var sortField = options.SortField?.Trim() ?? "memberCount";
        var sortDirection = options.SortDirection?.Trim().ToLowerInvariant() ?? "desc";
        var ascending = sortDirection == "asc";

        return sortField switch
        {
            "updatedAt" => ascending
                ? query.OrderBy(cluster => cluster.UpdatedAt)
                    .ThenByDescending(cluster => cluster.MemberCount)
                    .ThenBy(cluster => cluster.Title)
                : query.OrderByDescending(cluster => cluster.UpdatedAt)
                    .ThenByDescending(cluster => cluster.MemberCount)
                    .ThenBy(cluster => cluster.Title),
            "title" => ascending
                ? query.OrderBy(cluster => cluster.Title)
                    .ThenByDescending(cluster => cluster.MemberCount)
                    .ThenByDescending(cluster => cluster.UpdatedAt)
                : query.OrderByDescending(cluster => cluster.Title)
                    .ThenByDescending(cluster => cluster.MemberCount)
                    .ThenByDescending(cluster => cluster.UpdatedAt),
            _ => ascending
                ? query.OrderBy(cluster => cluster.MemberCount)
                    .ThenByDescending(cluster => cluster.UpdatedAt)
                    .ThenBy(cluster => cluster.Title)
                : query.OrderByDescending(cluster => cluster.MemberCount)
                    .ThenByDescending(cluster => cluster.UpdatedAt)
                    .ThenBy(cluster => cluster.Title)
        };
    }

    public async Task<IReadOnlyList<Guid>> GetStaleOwnerIdsAsync(DateTime staleBefore, int take)
    {
        return await _context.InsightClusters
            .AsNoTracking()
            .GroupBy(cluster => cluster.OwnerUserId)
            .Where(group => group.Max(cluster => cluster.LastComputedAt) < staleBefore)
            .OrderBy(group => group.Max(cluster => cluster.LastComputedAt))
            .Select(group => group.Key)
            .Take(take)
            .ToListAsync();
    }

    public async Task DeleteByOwnerAsync(Guid ownerUserId)
    {
        var existingClusters = await _context.InsightClusters
            .Where(cluster => cluster.OwnerUserId == ownerUserId)
            .ToListAsync();

        if (existingClusters.Count == 0)
        {
            return;
        }

        _context.InsightClusters.RemoveRange(existingClusters);
    }
}
