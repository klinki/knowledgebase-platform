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

public static class SearchSortDirections
{
    public const string Asc = "asc";
    public const string Desc = "desc";

    public static bool IsValid(string? value)
    {
        return string.Equals(value, Asc, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, Desc, StringComparison.OrdinalIgnoreCase);
    }
}

public static class ProcessedInsightSearchSortFields
{
    public const string Relevance = "relevance";
    public const string ProcessedAt = "processedAt";
    public const string Title = "title";
    public const string SourceUrl = "sourceUrl";

    public static bool IsValid(string? value)
    {
        return string.Equals(value, Relevance, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, ProcessedAt, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, Title, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, SourceUrl, StringComparison.OrdinalIgnoreCase);
    }
}

public static class CaptureSearchSortFields
{
    public const string Relevance = "relevance";
    public const string CreatedAt = "createdAt";
    public const string Status = "status";
    public const string ContentType = "contentType";
    public const string SourceUrl = "sourceUrl";

    public static bool IsValid(string? value)
    {
        return string.Equals(value, Relevance, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, CreatedAt, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, Status, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, ContentType, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, SourceUrl, StringComparison.OrdinalIgnoreCase);
    }
}

public class SearchRequestDto
{
    public string? Query { get; set; }
    public Guid? TopicClusterId { get; set; }
    public List<string> Tags { get; set; } = new();
    public string TagMatchMode { get; set; } = SearchMatchModes.Any;
    public List<LabelAssignmentDto> Labels { get; set; } = new();
    public string LabelMatchMode { get; set; } = SearchMatchModes.All;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public double Threshold { get; set; } = 0.6;
    public string? SortField { get; set; }
    public string? SortDirection { get; set; }
}

public class SearchResultDto
{
    public Guid Id { get; set; }
    public Guid CaptureId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<LabelAssignmentDto> Labels { get; set; } = new();
    public double? Similarity { get; set; }
}

public class SearchResultPageDto
{
    public List<SearchResultDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
