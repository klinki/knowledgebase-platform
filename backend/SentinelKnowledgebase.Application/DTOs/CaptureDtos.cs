using SentinelKnowledgebase.Domain.Enums;

namespace SentinelKnowledgebase.Application.DTOs;

public class CaptureRequest
{
    public string SourceUrl { get; set; } = string.Empty;
    public string RawContent { get; set; } = string.Empty;
    public CaptureSource Source { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class CaptureResponse
{
    public Guid Id { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public CaptureSource Source { get; set; }
    public CaptureStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InsightResponse
{
    public Guid Id { get; set; }
    public Guid RawCaptureId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string CleanContent { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class SemanticSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int Limit { get; set; } = 10;
}

public class SemanticSearchResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public double Similarity { get; set; }
}

public class TagSearchRequest
{
    public string Tag { get; set; } = string.Empty;
}

public class TagSearchResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<string> Tags { get; set; } = new();
}
