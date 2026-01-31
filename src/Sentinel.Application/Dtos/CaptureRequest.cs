using Sentinel.Domain.Enums;

namespace Sentinel.Application.Dtos;

public sealed class CaptureRequest
{
    public string SourceId { get; set; } = string.Empty;
    public CaptureSource Source { get; set; } = CaptureSource.Unknown;
    public string RawText { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? AuthorHandle { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
}
