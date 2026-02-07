using SentinelKnowledgebase.Application.DTOs.Capture;

namespace SentinelKnowledgebase.Application.Services.Interfaces;

public interface ICaptureService
{
    Task<CaptureResponseDto> CreateCaptureAsync(CaptureRequestDto request);
    Task<CaptureResponseDto?> GetCaptureByIdAsync(Guid id);
    Task<IEnumerable<CaptureResponseDto>> GetAllCapturesAsync();
    Task<PagedResult<CaptureResponseDto>> GetCapturesPagedAsync(int page, int pageSize);
    Task DeleteCaptureAsync(Guid id);
}

/// <summary>
/// Represents a paginated result set
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
