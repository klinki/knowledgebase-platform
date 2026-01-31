using Pgvector;

namespace SentinelKnowledgebase.Domain.Entities;

public class ProcessedInsight
{
    public Guid Id { get; set; }
    public Guid RawCaptureId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string CleanContent { get; set; } = string.Empty;
    public Vector? Embedding { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    
    public RawCapture RawCapture { get; set; } = null!;
}
