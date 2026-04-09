using SentinelKnowledgebase.Application.DTOs.Dashboard;
using SentinelKnowledgebase.Application.DTOs.Clusters;
using SentinelKnowledgebase.Application.DTOs.Labels;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInsightClusteringService _insightClusteringService;

    public DashboardService(IUnitOfWork unitOfWork, IInsightClusteringService insightClusteringService)
    {
        _unitOfWork = unitOfWork;
        _insightClusteringService = insightClusteringService;
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync(Guid ownerUserId)
    {
        var recentCaptures = await _unitOfWork.RawCaptures.GetRecentAsync(ownerUserId, 10);
        var topTags = await _unitOfWork.Tags.GetSummariesAsync(ownerUserId, 10);
        var topicClusters = await _insightClusteringService.GetClusterSummariesAsync(ownerUserId, 5);
        var totalCaptures = await _unitOfWork.RawCaptures.CountAsync(ownerUserId);
        var activeTags = await _unitOfWork.Tags.CountAsync(ownerUserId);

        return new DashboardOverviewDto
        {
            RecentCaptures = recentCaptures.Select(MapCapture).ToList(),
            TopTags = topTags.Select(MapTag).ToList(),
            TopicClusters = topicClusters.ToList(),
            Stats = new DashboardStatsDto
            {
                TotalCaptures = totalCaptures,
                ActiveTags = activeTags
            }
        };
    }

    public async Task<IEnumerable<TagSummaryDto>> GetTagSummariesAsync(Guid ownerUserId)
    {
        var tags = await _unitOfWork.Tags.GetSummariesAsync(ownerUserId);
        return tags.Select(MapTag);
    }

    private static CaptureListItemDto MapCapture(RawCapture capture)
    {
        return new CaptureListItemDto
        {
            Id = capture.Id,
            Title = capture.ProcessedInsight?.Title ?? capture.SourceUrl,
            SourceUrl = capture.SourceUrl,
            CapturedAt = capture.CreatedAt,
            Status = capture.Status,
            Tags = capture.Tags.Select(tag => tag.Name).ToList(),
            Labels = capture.LabelAssignments
                .OrderBy(assignment => assignment.LabelCategory.Name)
                .ThenBy(assignment => assignment.LabelValue.Value)
                .Select(assignment => new LabelAssignmentDto
                {
                    Category = assignment.LabelCategory.Name,
                    Value = assignment.LabelValue.Value
                })
                .ToList()
        };
    }

    private static TagSummaryDto MapTag(TagSummaryRecord tag)
    {
        return new TagSummaryDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Count = tag.Count,
            LastUsedAt = tag.LastUsedAt
        };
    }
}
