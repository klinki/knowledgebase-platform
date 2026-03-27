using System.ComponentModel.DataAnnotations.Schema;

namespace SentinelKnowledgebase.Domain.Entities;

public class RawCaptureLabelAssignment
{
    public Guid RawCaptureId { get; set; }

    [ForeignKey(nameof(RawCaptureId))]
    public RawCapture RawCapture { get; set; } = null!;

    public Guid LabelCategoryId { get; set; }

    [ForeignKey(nameof(LabelCategoryId))]
    public LabelCategory LabelCategory { get; set; } = null!;

    public Guid LabelValueId { get; set; }

    [ForeignKey(nameof(LabelValueId))]
    public LabelValue LabelValue { get; set; } = null!;
}
