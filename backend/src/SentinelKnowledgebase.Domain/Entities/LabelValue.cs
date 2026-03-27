using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SentinelKnowledgebase.Domain.Entities;

public class LabelValue
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid LabelCategoryId { get; set; }

    [ForeignKey(nameof(LabelCategoryId))]
    public LabelCategory LabelCategory { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Value { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<RawCaptureLabelAssignment> RawCaptureAssignments { get; set; } = new();

    public List<ProcessedInsightLabelAssignment> ProcessedInsightAssignments { get; set; } = new();
}
