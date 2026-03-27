using System.ComponentModel.DataAnnotations;

namespace SentinelKnowledgebase.Application.DTOs.Labels;

public class LabelAssignmentDto
{
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Value { get; set; } = string.Empty;
}

public class LabelCategoryRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

public class LabelValueRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Value { get; set; } = string.Empty;
}

public class LabelValueSummaryDto
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public int RawCaptureCount { get; set; }
    public int ProcessedInsightCount { get; set; }
}

public class LabelCategorySummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RawCaptureCount { get; set; }
    public int ProcessedInsightCount { get; set; }
    public List<LabelValueSummaryDto> Values { get; set; } = new();
}
