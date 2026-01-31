using NpgsqlTypes;
using Sentinel.Domain.Constants;
using Sentinel.Domain.Enums;

namespace Sentinel.Domain.Entities;

public sealed class ProcessedInsight
{
    public Guid Id { get; set; }
    public Guid RawCaptureId { get; set; }
    public RawCapture RawCapture { get; set; } = null!;
    public string Summary { get; set; } = string.Empty;
    public string Insight { get; set; } = string.Empty;
    public Sentiment Sentiment { get; set; }
    public string CleanText { get; set; } = string.Empty;
    public Vector Embedding { get; set; } = new(new float[EmbeddingConstants.Dimensions]);
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<ProcessedInsightTag> Tags { get; set; } = new List<ProcessedInsightTag>();
}
