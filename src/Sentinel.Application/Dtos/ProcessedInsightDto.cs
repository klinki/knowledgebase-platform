using Sentinel.Domain.Enums;

namespace Sentinel.Application.Dtos;

public sealed class ProcessedInsightDto
{
    public Guid InsightId { get; set; }
    public Guid RawCaptureId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Insight { get; set; } = string.Empty;
    public Sentiment Sentiment { get; set; }
    public string CleanText { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Tags { get; set; } = Array.Empty<string>();
    public DateTimeOffset CreatedAt { get; set; }
}
