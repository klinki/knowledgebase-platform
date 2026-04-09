using SentinelKnowledgebase.Application.DTOs.Labels;

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
    public List<LabelAssignmentDto> Labels { get; set; } = new();
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
    public List<LabelAssignmentDto> Labels { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
}

public class LabelSearchRequestDto
{
    public List<LabelAssignmentDto> Labels { get; set; } = new();
    public bool MatchAll { get; set; } = false;
}

public class LabelSearchResultDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<LabelAssignmentDto> Labels { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
}

public static class SearchMatchModes
{
    public const string Any = "any";
    public const string All = "all";

    public static bool IsValid(string? value)
    {
        return string.Equals(value, Any, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, All, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAll(string? value)
    {
        return string.Equals(value, All, StringComparison.OrdinalIgnoreCase);
    }
}

public class SearchRequestDto
{
    public string? Query { get; set; }
    public List<string> Tags { get; set; } = new();
    public string TagMatchMode { get; set; } = SearchMatchModes.Any;
    public List<LabelAssignmentDto> Labels { get; set; } = new();
    public string LabelMatchMode { get; set; } = SearchMatchModes.All;
    public int Limit { get; set; } = 20;
    public double Threshold { get; set; } = 0.3;
}

public class SearchResultDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<LabelAssignmentDto> Labels { get; set; } = new();
    public double? Similarity { get; set; }
}
