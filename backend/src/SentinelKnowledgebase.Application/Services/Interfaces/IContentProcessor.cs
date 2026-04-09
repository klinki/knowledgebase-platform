namespace SentinelKnowledgebase.Application.Services.Interfaces;

public class ContentInsights
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> KeyInsights { get; set; } = new();
    public List<string> ActionItems { get; set; } = new();
    public string? SourceTitle { get; set; }
    public string? Author { get; set; }
}

public class ClusterMetadata
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Keywords { get; set; } = new();
}

public interface IContentProcessor
{
    string DenoiseContent(string content);
    Task<ContentInsights> ExtractInsightsAsync(
        string content,
        Domain.Enums.ContentType contentType,
        string? outputLanguageCode = null);
    Task<float[]> GenerateEmbeddingAsync(string text);
    Task<ClusterMetadata> GenerateClusterMetadataAsync(IReadOnlyCollection<string> summaries);
}
