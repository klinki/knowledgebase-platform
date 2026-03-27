using SentinelKnowledgebase.Application.DTOs.Labels;

namespace SentinelKnowledgebase.Application.Services.Interfaces;

public interface ILabelService
{
    Task<IEnumerable<LabelCategorySummaryDto>> GetCategoriesAsync(Guid ownerUserId);
    Task<LabelCategorySummaryDto> CreateCategoryAsync(Guid ownerUserId, string name);
    Task<LabelCategorySummaryDto?> RenameCategoryAsync(Guid ownerUserId, Guid categoryId, string newName);
    Task<bool> DeleteCategoryAsync(Guid ownerUserId, Guid categoryId);
    Task<LabelValueSummaryDto?> CreateValueAsync(Guid ownerUserId, Guid categoryId, string value);
    Task<LabelValueSummaryDto?> RenameValueAsync(Guid ownerUserId, Guid valueId, string newValue);
    Task<bool> DeleteValueAsync(Guid ownerUserId, Guid valueId);
}
