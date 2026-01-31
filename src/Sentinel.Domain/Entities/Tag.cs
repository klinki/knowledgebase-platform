namespace Sentinel.Domain.Entities;

public sealed class Tag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<ProcessedInsightTag> Insights { get; set; } = new List<ProcessedInsightTag>();
}
