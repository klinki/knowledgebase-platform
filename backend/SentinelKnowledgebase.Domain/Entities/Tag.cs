namespace SentinelKnowledgebase.Domain.Entities;

public class Tag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<RawCapture> Captures { get; set; } = new();
}
