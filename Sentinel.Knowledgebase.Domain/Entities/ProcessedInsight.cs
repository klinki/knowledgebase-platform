using Sentinel.Knowledgebase.Domain.Enums;

namespace Sentinel.Knowledgebase.Domain.Entities;

public class ProcessedInsight : BaseEntity
{
    public Guid RawCaptureId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string KeyPoints { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Category { get; set; } = string.Empty;
    public float RelevanceScore { get; set; }
    public ProcessingStatus ProcessingStatus { get; set; } = ProcessingStatus.NotStarted;
    public string? ProcessingError { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? ProcessingCompletedAt { get; set; }
    
    // Embedding vector for semantic search (1536 dimensions for OpenAI embeddings)
    public float[]? Embedding { get; set; }
    
    // Navigation property
    public RawCapture RawCapture { get; set; } = null!;
}
