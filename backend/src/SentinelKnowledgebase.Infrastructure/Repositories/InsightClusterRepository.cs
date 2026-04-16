using Microsoft.EntityFrameworkCore;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public class InsightClusterRepository : IInsightClusterRepository
{
    private const string MemberSortFieldRank = "rank";
    private const string MemberSortFieldSimilarity = "similarity";
    private const string MemberSortFieldTitle = "title";
    private const string MemberSortFieldSourceUrl = "sourceUrl";
    private const string SortDirectionAsc = "asc";
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

    public async Task<TopicClusterDetailQueryResult?> GetDetailPagedAsync(
        Guid ownerUserId,
        Guid clusterId,
        TopicClusterDetailQueryOptions options)
    {
        var cluster = await _context.InsightClusters
            .AsNoTracking()
            .Where(item => item.OwnerUserId == ownerUserId && item.Id == clusterId)
            .Select(item => new TopicClusterDetailQueryResult
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                KeywordsJson = item.KeywordsJson,
                MemberCount = item.MemberCount,
                UpdatedAt = item.UpdatedAt
            })
            .FirstOrDefaultAsync();
        if (cluster == null)
        {
            return null;
        }

        var page = Math.Max(1, options.Page);
        var pageSize = Math.Clamp(options.PageSize, 1, 100);
        var sortField = options.SortField?.Trim() ?? MemberSortFieldRank;
        var sortDirection = options.SortDirection?.Trim().ToLowerInvariant() ?? SortDirectionAsc;

        var membersQuery = _context.InsightClusterMemberships
            .AsNoTracking()
            .Where(membership =>
                membership.InsightClusterId == clusterId &&
                membership.InsightCluster.OwnerUserId == ownerUserId);
        var totalCount = await membersQuery.CountAsync();

        var sortedMembersQuery = ApplyMemberSorting(membersQuery, sortField, sortDirection);
        var members = await sortedMembersQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(membership => new TopicClusterMemberRecord
            {
                CaptureId = membership.ProcessedInsight.RawCaptureId,
                ProcessedInsightId = membership.ProcessedInsightId,
                Title = membership.ProcessedInsight.Title,
                Summary = membership.ProcessedInsight.Summary,
                SourceUrl = membership.ProcessedInsight.RawCapture.SourceUrl,
                Rank = membership.Rank,
                SimilarityToCentroid = membership.SimilarityToCentroid,
                Tags = membership.ProcessedInsight.Tags.Select(tag => tag.Name).ToList(),
                Labels = membership.ProcessedInsight.LabelAssignments
                    .OrderBy(assignment => assignment.LabelCategory.Name)
                    .ThenBy(assignment => assignment.LabelValue.Value)
                    .Select(assignment => new LabelRecord
                    {
                        Category = assignment.LabelCategory.Name,
                        Value = assignment.LabelValue.Value
                    })
                    .ToList()
            })
            .ToListAsync();

        cluster.MembersPage = page;
        cluster.MembersPageSize = pageSize;
        cluster.MembersTotalCount = totalCount;
        cluster.MembersSortField = sortField;
        cluster.MembersSortDirection = sortDirection;
        cluster.Members = members;
        return cluster;
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

    private static IOrderedQueryable<InsightClusterMembership> ApplyMemberSorting(
        IQueryable<InsightClusterMembership> query,
        string sortField,
        string sortDirection)
    {
        var ascending = string.Equals(sortDirection, SortDirectionAsc, StringComparison.OrdinalIgnoreCase);

        return sortField switch
        {
            MemberSortFieldSimilarity => ascending
                ? query.OrderBy(membership => membership.SimilarityToCentroid)
                    .ThenBy(membership => membership.ProcessedInsightId)
                : query.OrderByDescending(membership => membership.SimilarityToCentroid)
                    .ThenBy(membership => membership.ProcessedInsightId),
            MemberSortFieldTitle => ascending
                ? query.OrderBy(membership => membership.ProcessedInsight.Title)
                    .ThenBy(membership => membership.ProcessedInsightId)
                : query.OrderByDescending(membership => membership.ProcessedInsight.Title)
                    .ThenBy(membership => membership.ProcessedInsightId),
            MemberSortFieldSourceUrl => ascending
                ? query.OrderBy(membership => membership.ProcessedInsight.RawCapture.SourceUrl)
                    .ThenBy(membership => membership.ProcessedInsightId)
                : query.OrderByDescending(membership => membership.ProcessedInsight.RawCapture.SourceUrl)
                    .ThenBy(membership => membership.ProcessedInsightId),
            _ => ascending
                ? query.OrderBy(membership => membership.Rank)
                    .ThenBy(membership => membership.ProcessedInsightId)
                : query.OrderByDescending(membership => membership.Rank)
                    .ThenBy(membership => membership.ProcessedInsightId)
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
