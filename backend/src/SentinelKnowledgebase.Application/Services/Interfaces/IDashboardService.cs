using SentinelKnowledgebase.Application.DTOs.Dashboard;

namespace SentinelKnowledgebase.Application.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardOverviewDto> GetOverviewAsync();
    Task<IEnumerable<TagSummaryDto>> GetTagSummariesAsync();
}
