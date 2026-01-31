namespace SentinelKnowledgebase.Domain.Entities;

public class RawCapture
{
    public Guid Id { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string RawContent { get; set; } = string.Empty;
    public Enums.CaptureSource Source { get; set; }
    public Enums.CaptureStatus Status { get; set; } = Enums.CaptureStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    
    public ProcessedInsight? ProcessedInsight { get; set; }
    public List<Tag> Tags { get; set; } = new();
}
