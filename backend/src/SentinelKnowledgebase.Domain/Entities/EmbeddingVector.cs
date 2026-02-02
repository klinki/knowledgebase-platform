using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentinelKnowledgebase.Domain.Entities;

public class EmbeddingVector
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid ProcessedInsightId { get; set; }
    
    [ForeignKey(nameof(ProcessedInsightId))]
    public ProcessedInsight ProcessedInsight { get; set; } = null!;
    
    [Required]
    [Column(TypeName = "vector(1536)")]
    public float[] Vector { get; set; } = Array.Empty<float>();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
