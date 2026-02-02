using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

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
    public Vector Vector { get; set; } = new Vector(Array.Empty<float>());

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
