using Sentinel.Domain.Enums;

namespace Sentinel.Application.Dtos;

public sealed class CaptureResponse
{
    public Guid CaptureId { get; set; }
    public ProcessingStatus Status { get; set; }
}
