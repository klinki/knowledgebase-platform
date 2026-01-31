using Sentinel.Domain.Enums;

namespace Sentinel.Application.Models;

public sealed class InsightExtractionResult
{
    public string Summary { get; init; } = string.Empty;
    public string Insight { get; init; } = string.Empty;
    public Sentiment Sentiment { get; init; }
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();
}
