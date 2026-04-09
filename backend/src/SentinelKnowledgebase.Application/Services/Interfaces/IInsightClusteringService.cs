using SentinelKnowledgebase.Application.DTOs.Clusters;

namespace SentinelKnowledgebase.Application.Services.Interfaces;

public interface IInsightClusteringService
{
    Task RebuildOwnerClustersAsync(Guid ownerUserId);
    Task RebuildStaleOwnerClustersAsync();
    Task<IReadOnlyList<TopicClusterSummaryDto>> GetClusterSummariesAsync(Guid ownerUserId, int take = 5);
    Task<TopicClusterDetailDto?> GetClusterDetailAsync(Guid ownerUserId, Guid clusterId);
}
