using SentinelKnowledgebase.Application.DTOs.Dashboard;

namespace SentinelKnowledgebase.Application.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardOverviewDto> GetOverviewAsync(Guid ownerUserId);
    Task<IEnumerable<TagSummaryDto>> GetTagSummariesAsync(Guid ownerUserId);
}
