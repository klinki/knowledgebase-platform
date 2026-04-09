using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentinelKnowledgebase.Domain.Entities;

public class InsightCluster
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid OwnerUserId { get; set; }

    [Required]
    [MaxLength(60)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? Description { get; set; }

    public string KeywordsJson { get; set; } = "[]";

    public int MemberCount { get; set; }

    public Guid? RepresentativeProcessedInsightId { get; set; }

    [ForeignKey(nameof(RepresentativeProcessedInsightId))]
    public ProcessedInsight? RepresentativeProcessedInsight { get; set; }

    public DateTime LastComputedAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<InsightClusterMembership> Memberships { get; set; } = new();
}
