using Sentinel.Domain.Enums;

namespace Sentinel.Application.Dtos;

public sealed class CaptureDetailsResponse
{
    public Guid CaptureId { get; set; }
    public ProcessingStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public ProcessedInsightDto? Insight { get; set; }
}
