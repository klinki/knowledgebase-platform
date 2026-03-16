using SentinelKnowledgebase.Application.DTOs.Dashboard;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Application.Services;

public class TagService : ITagService
{
    private readonly IUnitOfWork _unitOfWork;

    public TagService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TagSummaryDto> CreateTagAsync(Guid ownerUserId, string name)
    {
        var normalized = name.Trim();

        var existing = await _unitOfWork.Tags.GetByNameAsync(ownerUserId, normalized);
        if (existing != null)
        {
            throw new InvalidOperationException($"A tag with the name '{normalized}' already exists.");
        }

        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Name = normalized,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Tags.AddAsync(tag);
        await _unitOfWork.SaveChangesAsync();

        return new TagSummaryDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Count = 0,
            LastUsedAt = null
        };
    }

    public async Task<TagSummaryDto?> RenameTagAsync(Guid ownerUserId, Guid tagId, string newName)
    {
        var tag = await _unitOfWork.Tags.GetByIdAsync(tagId);
        if (tag == null || tag.OwnerUserId != ownerUserId)
        {
            return null;
        }

        var normalized = newName.Trim();

        if (!string.Equals(tag.Name, normalized, StringComparison.OrdinalIgnoreCase))
        {
            var conflict = await _unitOfWork.Tags.GetByNameAsync(ownerUserId, normalized);
            if (conflict != null)
            {
                throw new InvalidOperationException($"A tag with the name '{normalized}' already exists.");
            }
        }

        tag.Name = normalized;
        await _unitOfWork.Tags.UpdateAsync(tag);
        await _unitOfWork.SaveChangesAsync();

        var summaries = await _unitOfWork.Tags.GetSummariesAsync(ownerUserId);
        var updated = summaries.FirstOrDefault(s => s.Id == tagId);

        return new TagSummaryDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Count = updated?.Count ?? 0,
            LastUsedAt = updated?.LastUsedAt
        };
    }

    public async Task<bool> DeleteTagAsync(Guid ownerUserId, Guid tagId)
    {
        var tag = await _unitOfWork.Tags.GetByIdAsync(tagId);
        if (tag == null || tag.OwnerUserId != ownerUserId)
        {
            return false;
        }

        await _unitOfWork.Tags.DeleteAsync(tagId);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
