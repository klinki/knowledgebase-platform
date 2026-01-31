using Sentinel.Domain.Enums;

namespace Sentinel.Domain.Entities;

public sealed class RawCapture
{
    public Guid Id { get; set; }
    public string SourceId { get; set; } = string.Empty;
    public CaptureSource Source { get; set; }
    public string RawText { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? AuthorHandle { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
    public ProcessingStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public ProcessedInsight? ProcessedInsight { get; set; }
}
