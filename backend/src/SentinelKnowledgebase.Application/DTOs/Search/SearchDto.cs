namespace SentinelKnowledgebase.Application.DTOs.Search;

public class SemanticSearchRequestDto
{
    public string Query { get; set; } = string.Empty;
    public int TopK { get; set; } = 5;
    public double Threshold { get; set; } = 0.5;
}

public class SemanticSearchResultDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public double Similarity { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class TagSearchRequestDto
{
    public List<string> Tags { get; set; } = new();
    public bool MatchAll { get; set; } = false;
}

public class TagSearchResultDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
}
