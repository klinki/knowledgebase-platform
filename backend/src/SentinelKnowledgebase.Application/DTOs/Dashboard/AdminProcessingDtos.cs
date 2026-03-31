namespace SentinelKnowledgebase.Application.DTOs.Dashboard;

public class CaptureProcessingAdminOverviewDto
{
    public bool IsPaused { get; set; }
    public DateTimeOffset? ChangedAt { get; set; }
    public string? ChangedByDisplayName { get; set; }
    public CaptureProcessingCaptureCountsDto CaptureCounts { get; set; } = new();
    public CaptureProcessingJobCountsDto JobCounts { get; set; } = new();
    public List<CaptureListItemDto> RecentCaptures { get; set; } = new();
}

public class CaptureProcessingCaptureCountsDto
{
    public int Pending { get; set; }
    public int Processing { get; set; }
    public int Completed { get; set; }
    public int Failed { get; set; }
}

public class CaptureProcessingJobCountsDto
{
    public long Enqueued { get; set; }
    public long Scheduled { get; set; }
    public long Processing { get; set; }
    public long Failed { get; set; }
}
