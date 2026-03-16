using SentinelKnowledgebase.Application.DTOs.Dashboard;

namespace SentinelKnowledgebase.Application.Services.Interfaces;

public interface ITagService
{
    Task<TagSummaryDto> CreateTagAsync(Guid ownerUserId, string name);
    Task<TagSummaryDto?> RenameTagAsync(Guid ownerUserId, Guid tagId, string newName);
    Task<bool> DeleteTagAsync(Guid ownerUserId, Guid tagId);
}
