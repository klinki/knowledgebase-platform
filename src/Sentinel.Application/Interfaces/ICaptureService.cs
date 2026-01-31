using Sentinel.Application.Dtos;

namespace Sentinel.Application.Interfaces;

public interface ICaptureService
{
    Task<CaptureResponse> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken);
    Task<CaptureDetailsResponse?> GetCaptureAsync(Guid captureId, CancellationToken cancellationToken);
}
