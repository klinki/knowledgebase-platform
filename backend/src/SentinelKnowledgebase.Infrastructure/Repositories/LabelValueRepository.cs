using Microsoft.EntityFrameworkCore;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public class LabelValueRepository : ILabelValueRepository
{
    private readonly ApplicationDbContext _context;

    public LabelValueRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<LabelValue> AddAsync(LabelValue value)
    {
        _context.LabelValues.Add(value);
        return Task.FromResult(value);
    }

    public async Task<LabelValue?> GetByIdAsync(Guid id)
    {
        return await _context.LabelValues
            .Include(value => value.LabelCategory)
            .FirstOrDefaultAsync(value => value.Id == id);
    }

    public async Task<LabelValue?> GetByCategoryAndValueAsync(Guid categoryId, string value)
    {
        var normalized = value.Trim().ToLower();
        return await _context.LabelValues
            .Include(labelValue => labelValue.LabelCategory)
            .FirstOrDefaultAsync(labelValue =>
                labelValue.LabelCategoryId == categoryId &&
                labelValue.Value.ToLower() == normalized);
    }

    public Task UpdateAsync(LabelValue value)
    {
        _context.LabelValues.Update(value);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var value = await _context.LabelValues.FindAsync(id);
        if (value != null)
        {
            _context.LabelValues.Remove(value);
        }
    }
}
