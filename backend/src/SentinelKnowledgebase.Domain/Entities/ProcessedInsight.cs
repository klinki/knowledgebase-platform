using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentinelKnowledgebase.Domain.Entities;

public class ProcessedInsight
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid RawCaptureId { get; set; }
    
    [ForeignKey(nameof(RawCaptureId))]
    public RawCapture RawCapture { get; set; } = null!;
    
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Summary { get; set; } = string.Empty;
    
    public string? KeyInsights { get; set; }
    
    public string? ActionItems { get; set; }
    
    [MaxLength(100)]
    public string? SourceTitle { get; set; }
    
    [MaxLength(500)]
    public string? Author { get; set; }
    
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    
    public List<Tag> Tags { get; set; } = new();
    
    public EmbeddingVector? EmbeddingVector { get; set; }
}
