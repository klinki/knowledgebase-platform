using SentinelKnowledgebase.Application.DTOs.Capture;

namespace SentinelKnowledgebase.Application.Services.Interfaces;

public interface ICaptureService
{
    Task<CaptureResponseDto> CreateCaptureAsync(Guid ownerUserId, CaptureRequestDto request);
    Task<IReadOnlyList<CaptureResponseDto>> CreateCapturesAsync(Guid ownerUserId, IReadOnlyList<CaptureRequestDto> requests);
    Task ProcessCaptureAsync(Guid rawCaptureId);
    Task<CaptureResponseDto?> GetCaptureByIdAsync(Guid ownerUserId, Guid id);
    Task<CaptureListPageDto> GetCaptureListPageAsync(Guid ownerUserId, CaptureListQueryDto query);
    Task<IEnumerable<CaptureResponseDto>> GetAllCapturesAsync(Guid ownerUserId);
    Task<bool> DeleteCaptureAsync(Guid ownerUserId, Guid id);
    Task<bool> RetryCaptureAsync(Guid ownerUserId, Guid id);
    Task<IReadOnlyList<Guid>> RetryFailedCapturesAsync(Guid ownerUserId, IReadOnlyCollection<Guid> ids);
    Task<IReadOnlyList<Guid>> RetryAllFailedCapturesAsync(Guid ownerUserId, CaptureBulkRetryRequestDto request);
}
