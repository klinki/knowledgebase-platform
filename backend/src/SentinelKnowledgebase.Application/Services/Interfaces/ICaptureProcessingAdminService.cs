using SentinelKnowledgebase.Application.DTOs.Dashboard;

namespace SentinelKnowledgebase.Application.Services.Interfaces;

public interface ICaptureProcessingAdminService
{
    Task<bool> IsPausedAsync();
    Task<CaptureProcessingAdminOverviewDto> GetOverviewAsync();
    Task<CaptureProcessingAdminOverviewDto> PauseAsync(Guid changedByUserId);
    Task<CaptureProcessingAdminOverviewDto> ResumeAsync(Guid changedByUserId);
}
