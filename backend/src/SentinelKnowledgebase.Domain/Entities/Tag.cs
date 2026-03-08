using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentinelKnowledgebase.Domain.Entities;

/// <summary>
/// Tag entity used for categorization of insights.
/// </summary>
public class Tag
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid OwnerUserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public List<RawCapture> RawCaptures { get; set; } = new();
    
    public List<ProcessedInsight> ProcessedInsights { get; set; } = new();
}
