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
