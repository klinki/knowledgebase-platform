namespace Sentinel.Knowledgebase.Domain.Entities;

public class SearchHistory : BaseEntity
{
    public string Query { get; set; } = string.Empty;
    public string SearchType { get; set; } = string.Empty; // "semantic" or "tags"
    public int ResultCount { get; set; }
    public string? UserId { get; set; }
    public Dictionary<string, string> SearchParameters { get; set; } = new();
}
