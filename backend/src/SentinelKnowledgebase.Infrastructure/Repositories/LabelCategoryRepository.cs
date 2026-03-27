using Microsoft.EntityFrameworkCore;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public class LabelCategoryRepository : ILabelCategoryRepository
{
    private readonly ApplicationDbContext _context;

    public LabelCategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<LabelCategory> AddAsync(LabelCategory category)
    {
        _context.LabelCategories.Add(category);
        return Task.FromResult(category);
    }

    public async Task<LabelCategory?> GetByIdAsync(Guid id)
    {
        return await _context.LabelCategories.FindAsync(id);
    }

    public async Task<LabelCategory?> GetByIdWithValuesAsync(Guid id)
    {
        return await _context.LabelCategories
            .Include(category => category.Values)
                .ThenInclude(value => value.RawCaptureAssignments)
            .Include(category => category.Values)
                .ThenInclude(value => value.ProcessedInsightAssignments)
            .FirstOrDefaultAsync(category => category.Id == id);
    }

    public async Task<LabelCategory?> GetByNameAsync(Guid ownerUserId, string name)
    {
        var normalized = name.Trim().ToLower();
        return await _context.LabelCategories
            .FirstOrDefaultAsync(category =>
                category.OwnerUserId == ownerUserId &&
                category.Name.ToLower() == normalized);
    }

    public async Task<IEnumerable<LabelCategory>> GetAllWithValuesAsync(Guid ownerUserId)
    {
        return await _context.LabelCategories
            .AsNoTracking()
            .Where(category => category.OwnerUserId == ownerUserId)
            .Include(category => category.Values)
                .ThenInclude(value => value.RawCaptureAssignments)
            .Include(category => category.Values)
                .ThenInclude(value => value.ProcessedInsightAssignments)
            .OrderBy(category => category.Name)
            .ToListAsync();
    }

    public Task UpdateAsync(LabelCategory category)
    {
        _context.LabelCategories.Update(category);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await _context.LabelCategories.FindAsync(id);
        if (category != null)
        {
            _context.LabelCategories.Remove(category);
        }
    }
}
