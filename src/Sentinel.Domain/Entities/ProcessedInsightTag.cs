namespace Sentinel.Domain.Entities;

public sealed class ProcessedInsightTag
{
    public Guid ProcessedInsightId { get; set; }
    public ProcessedInsight ProcessedInsight { get; set; } = null!;
    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
