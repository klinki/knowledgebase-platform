using Sentinel.Knowledgebase.Domain.Enums;

namespace Sentinel.Knowledgebase.Domain.Entities;

public class RawCapture : BaseEntity
{
    public string SourceUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public ContentType ContentType { get; set; }
    public string? Author { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public CaptureStatus Status { get; set; } = CaptureStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime? ProcessedAt { get; set; }
    
    // Navigation property
    public ProcessedInsight? ProcessedInsight { get; set; }
}
