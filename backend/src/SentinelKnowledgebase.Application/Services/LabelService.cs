using SentinelKnowledgebase.Application.DTOs.Labels;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Application.Services;

public class LabelService : ILabelService
{
    private readonly IUnitOfWork _unitOfWork;

    public LabelService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<LabelCategorySummaryDto>> GetCategoriesAsync(Guid ownerUserId)
    {
        var categories = await _unitOfWork.LabelCategories.GetAllWithValuesAsync(ownerUserId);
        return categories.Select(MapCategory);
    }

    public async Task<LabelCategorySummaryDto> CreateCategoryAsync(Guid ownerUserId, string name)
    {
        var normalized = name.Trim();

        var existing = await _unitOfWork.LabelCategories.GetByNameAsync(ownerUserId, normalized);
        if (existing != null)
        {
            throw new InvalidOperationException($"A label category named '{normalized}' already exists.");
        }

        var category = new LabelCategory
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Name = normalized,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.LabelCategories.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return MapCategory(category);
    }

    public async Task<LabelCategorySummaryDto?> RenameCategoryAsync(Guid ownerUserId, Guid categoryId, string newName)
    {
        var category = await _unitOfWork.LabelCategories.GetByIdAsync(categoryId);
        if (category == null || category.OwnerUserId != ownerUserId)
        {
            return null;
        }

        var normalized = newName.Trim();

        if (!string.Equals(category.Name, normalized, StringComparison.OrdinalIgnoreCase))
        {
            var conflict = await _unitOfWork.LabelCategories.GetByNameAsync(ownerUserId, normalized);
            if (conflict != null)
            {
                throw new InvalidOperationException($"A label category named '{normalized}' already exists.");
            }
        }

        category.Name = normalized;
        await _unitOfWork.LabelCategories.UpdateAsync(category);
        await _unitOfWork.SaveChangesAsync();

        var updated = await _unitOfWork.LabelCategories.GetByIdWithValuesAsync(categoryId);
        return updated == null ? null : MapCategory(updated);
    }

    public async Task<bool> DeleteCategoryAsync(Guid ownerUserId, Guid categoryId)
    {
        var category = await _unitOfWork.LabelCategories.GetByIdWithValuesAsync(categoryId);
        if (category == null || category.OwnerUserId != ownerUserId)
        {
            return false;
        }

        foreach (var value in category.Values.ToList())
        {
            await _unitOfWork.LabelValues.DeleteAsync(value.Id);
        }

        await _unitOfWork.LabelCategories.DeleteAsync(categoryId);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<LabelValueSummaryDto?> CreateValueAsync(Guid ownerUserId, Guid categoryId, string value)
    {
        var category = await _unitOfWork.LabelCategories.GetByIdAsync(categoryId);
        if (category == null || category.OwnerUserId != ownerUserId)
        {
            return null;
        }

        var normalized = value.Trim();
        var existing = await _unitOfWork.LabelValues.GetByCategoryAndValueAsync(categoryId, normalized);
        if (existing != null)
        {
            throw new InvalidOperationException($"A label value '{normalized}' already exists in category '{category.Name}'.");
        }

        var labelValue = new LabelValue
        {
            Id = Guid.NewGuid(),
            LabelCategoryId = categoryId,
            Value = normalized,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.LabelValues.AddAsync(labelValue);
        await _unitOfWork.SaveChangesAsync();

        return MapValue(labelValue);
    }

    public async Task<LabelValueSummaryDto?> RenameValueAsync(Guid ownerUserId, Guid valueId, string newValue)
    {
        var value = await _unitOfWork.LabelValues.GetByIdAsync(valueId);
        if (value == null || value.LabelCategory.OwnerUserId != ownerUserId)
        {
            return null;
        }

        var normalized = newValue.Trim();
        if (!string.Equals(value.Value, normalized, StringComparison.OrdinalIgnoreCase))
        {
            var conflict = await _unitOfWork.LabelValues.GetByCategoryAndValueAsync(value.LabelCategoryId, normalized);
            if (conflict != null)
            {
                throw new InvalidOperationException(
                    $"A label value '{normalized}' already exists in category '{value.LabelCategory.Name}'.");
            }
        }

        value.Value = normalized;
        await _unitOfWork.LabelValues.UpdateAsync(value);
        await _unitOfWork.SaveChangesAsync();

        var category = await _unitOfWork.LabelCategories.GetByIdWithValuesAsync(value.LabelCategoryId);
        var updated = category?.Values.FirstOrDefault(item => item.Id == valueId);
        return updated == null ? null : MapValue(updated);
    }

    public async Task<bool> DeleteValueAsync(Guid ownerUserId, Guid valueId)
    {
        var value = await _unitOfWork.LabelValues.GetByIdAsync(valueId);
        if (value == null || value.LabelCategory.OwnerUserId != ownerUserId)
        {
            return false;
        }

        await _unitOfWork.LabelValues.DeleteAsync(valueId);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private static LabelCategorySummaryDto MapCategory(LabelCategory category)
    {
        var values = category.Values
            .OrderBy(value => value.Value)
            .Select(MapValue)
            .ToList();

        return new LabelCategorySummaryDto
        {
            Id = category.Id,
            Name = category.Name,
            RawCaptureCount = values.Sum(value => value.RawCaptureCount),
            ProcessedInsightCount = values.Sum(value => value.ProcessedInsightCount),
            Values = values
        };
    }

    private static LabelValueSummaryDto MapValue(LabelValue value)
    {
        return new LabelValueSummaryDto
        {
            Id = value.Id,
            Value = value.Value,
            RawCaptureCount = value.RawCaptureAssignments.Count,
            ProcessedInsightCount = value.ProcessedInsightAssignments.Count
        };
    }
}
