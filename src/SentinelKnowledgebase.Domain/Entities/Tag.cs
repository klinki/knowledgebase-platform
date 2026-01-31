using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentinelKnowledgebase.Domain.Entities;

public class Tag
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public List<RawCapture> RawCaptures { get; set; } = new();
    
    public List<ProcessedInsight> ProcessedInsights { get; set; } = new();
}
