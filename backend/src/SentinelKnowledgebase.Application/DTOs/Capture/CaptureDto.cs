using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Application.DTOs.Labels;

namespace SentinelKnowledgebase.Application.DTOs.Capture;

public class CaptureRequestDto
{
    public string SourceUrl { get; set; } = string.Empty;
    public ContentType ContentType { get; set; }
    public string RawContent { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public List<string>? Tags { get; set; }
    public List<LabelAssignmentDto>? Labels { get; set; }
}

public class CaptureResponseDto
{
    public Guid Id { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public ContentType ContentType { get; set; }
    public CaptureStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string RawContent { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public string? FailureReason { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<LabelAssignmentDto> Labels { get; set; } = new();
    public ProcessedInsightDto? ProcessedInsight { get; set; }
}

public class CaptureListQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortField { get; set; }
    public string? SortDirection { get; set; }
    public string? ContentType { get; set; }
    public string? Status { get; set; }
}

public class CaptureListPageDto
{
    public List<CaptureListItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class CaptureListItemDto
{
    public Guid Id { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public ContentType ContentType { get; set; }
    public CaptureStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
}

public class CaptureAcceptedDto
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ProcessedInsightDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? KeyInsights { get; set; }
    public string? ActionItems { get; set; }
    public string? SourceTitle { get; set; }
    public string? Author { get; set; }
    public DateTime ProcessedAt { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<LabelAssignmentDto> Labels { get; set; } = new();
}
