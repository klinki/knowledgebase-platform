using SentinelKnowledgebase.Application.DTOs.Dashboard;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync()
    {
        var recentCaptures = await _unitOfWork.RawCaptures.GetRecentAsync(10);
        var topTags = await _unitOfWork.Tags.GetSummariesAsync(10);
        var totalCaptures = await _unitOfWork.RawCaptures.CountAsync();
        var activeTags = await _unitOfWork.Tags.CountAsync();

        return new DashboardOverviewDto
        {
            RecentCaptures = recentCaptures.Select(MapCapture).ToList(),
            TopTags = topTags.Select(MapTag).ToList(),
            Stats = new DashboardStatsDto
            {
                TotalCaptures = totalCaptures,
                ActiveTags = activeTags
            }
        };
    }

    public async Task<IEnumerable<TagSummaryDto>> GetTagSummariesAsync()
    {
        var tags = await _unitOfWork.Tags.GetSummariesAsync();
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
            Tags = capture.Tags.Select(tag => tag.Name).ToList()
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
