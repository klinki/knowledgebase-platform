using Microsoft.EntityFrameworkCore;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public class TagRepository : ITagRepository
{
    private readonly ApplicationDbContext _context;
    
    public TagRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public Task<Tag> AddAsync(Tag tag)
    {
        _context.Tags.Add(tag);
        return Task.FromResult(tag);
    }
    
    public async Task<Tag?> GetByIdAsync(Guid id)
    {
        return await _context.Tags.FindAsync(id);
    }
    
    public async Task<Tag?> GetByNameAsync(Guid ownerUserId, string name)
    {
        return await _context.Tags.FirstOrDefaultAsync(t => t.OwnerUserId == ownerUserId && t.Name == name);
    }
    
    public async Task<IEnumerable<Tag>> GetAllAsync(Guid ownerUserId)
    {
        return await _context.Tags
            .Where(tag => tag.OwnerUserId == ownerUserId)
            .ToListAsync();
    }

    public async Task<IEnumerable<TagSummaryRecord>> GetSummariesAsync(Guid ownerUserId, int? take = null, bool includeZeroCount = false)
    {
        IQueryable<Tag> query = _context.Tags
            .Where(tag => tag.OwnerUserId == ownerUserId);

        if (!includeZeroCount)
        {
            query = query.Where(tag => tag.RawCaptures.Any());
        }

        IQueryable<TagSummaryRecord> summaryQuery = query
            .Select(tag => new TagSummaryRecord
            {
                Id = tag.Id,
                Name = tag.Name,
                Count = tag.RawCaptures.Count,
                LastUsedAt = tag.RawCaptures
                    .OrderByDescending(capture => capture.CreatedAt)
                    .Select(capture => (DateTime?)capture.CreatedAt)
                    .FirstOrDefault()
            })
            .OrderByDescending(tag => tag.Count)
            .ThenByDescending(tag => tag.LastUsedAt)
            .ThenBy(tag => tag.Name);

        if (take.HasValue)
        {
            summaryQuery = summaryQuery.Take(take.Value);
        }

        return await summaryQuery.ToListAsync();
    }

    public Task<int> CountAsync(Guid ownerUserId)
    {
        return _context.Tags.CountAsync(tag => tag.OwnerUserId == ownerUserId && tag.RawCaptures.Any());
    }
    
    public Task UpdateAsync(Tag tag)
    {
        _context.Tags.Update(tag);
        return Task.CompletedTask;
    }
    
    public async Task DeleteAsync(Guid id)
    {
        var tag = await _context.Tags.FindAsync(id);
        if (tag != null)
        {
            _context.Tags.Remove(tag);
        }
    }
}
