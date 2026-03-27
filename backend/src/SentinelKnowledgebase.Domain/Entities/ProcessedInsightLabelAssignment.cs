using System.ComponentModel.DataAnnotations.Schema;

namespace SentinelKnowledgebase.Domain.Entities;

public class ProcessedInsightLabelAssignment
{
    public Guid ProcessedInsightId { get; set; }

    [ForeignKey(nameof(ProcessedInsightId))]
    public ProcessedInsight ProcessedInsight { get; set; } = null!;

    public Guid LabelCategoryId { get; set; }

    [ForeignKey(nameof(LabelCategoryId))]
    public LabelCategory LabelCategory { get; set; } = null!;

    public Guid LabelValueId { get; set; }

    [ForeignKey(nameof(LabelValueId))]
    public LabelValue LabelValue { get; set; } = null!;
}
