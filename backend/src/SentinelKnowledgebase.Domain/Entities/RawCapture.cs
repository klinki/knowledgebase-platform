using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SentinelKnowledgebase.Domain.Enums;

namespace SentinelKnowledgebase.Domain.Entities;

public class RawCapture
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(2048)]
    public string SourceUrl { get; set; } = string.Empty;
    
    [Required]
    public ContentType ContentType { get; set; }
    
    [Required]
    public string RawContent { get; set; } = string.Empty;
    
    public string? Metadata { get; set; }
    
    public CaptureStatus Status { get; set; } = CaptureStatus.Pending;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ProcessedAt { get; set; }
    
    public List<Tag> Tags { get; set; } = new();
    
    public ProcessedInsight? ProcessedInsight { get; set; }
}
