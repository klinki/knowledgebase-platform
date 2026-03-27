using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Application.DTOs.Labels;

namespace SentinelKnowledgebase.Application.DTOs.Dashboard;

public class DashboardOverviewDto
{
    public List<CaptureListItemDto> RecentCaptures { get; set; } = new();
    public List<TagSummaryDto> TopTags { get; set; } = new();
    public DashboardStatsDto Stats { get; set; } = new();
}

public class CaptureListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
    public CaptureStatus Status { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<LabelAssignmentDto> Labels { get; set; } = new();
}

public class TagSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public class DashboardStatsDto
{
    public int TotalCaptures { get; set; }
    public int ActiveTags { get; set; }
}
