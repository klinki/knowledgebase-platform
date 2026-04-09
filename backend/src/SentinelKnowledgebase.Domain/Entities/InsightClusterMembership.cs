using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentinelKnowledgebase.Domain.Entities;

public class InsightClusterMembership
{
    [Required]
    public Guid InsightClusterId { get; set; }

    [ForeignKey(nameof(InsightClusterId))]
    public InsightCluster InsightCluster { get; set; } = null!;

    [Key]
    [Required]
    public Guid ProcessedInsightId { get; set; }

    [ForeignKey(nameof(ProcessedInsightId))]
    public ProcessedInsight ProcessedInsight { get; set; } = null!;

    public int Rank { get; set; }

    public double SimilarityToCentroid { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
