using SentinelKnowledgebase.Application.DTOs.Capture;

namespace SentinelKnowledgebase.Application.Services.Interfaces;

public interface ICaptureService
{
    Task<CaptureResponseDto> CreateCaptureAsync(CaptureRequestDto request);
    Task ProcessCaptureAsync(Guid rawCaptureId);
    Task<CaptureResponseDto?> GetCaptureByIdAsync(Guid id);
    Task<IEnumerable<CaptureResponseDto>> GetAllCapturesAsync();
    Task DeleteCaptureAsync(Guid id);
}
