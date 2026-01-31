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

    public async Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tags
            .Include(t => t.Captures)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Tags
            .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
    }

    public async Task<Tag> AddAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync(cancellationToken);
        return tag;
    }

    public async Task<IEnumerable<Tag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tags.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<RawCapture>> GetCapturesByTagAsync(string tagName, CancellationToken cancellationToken = default)
    {
        var tag = await _context.Tags
            .Include(t => t.Captures)
            .ThenInclude(c => c.ProcessedInsight)
            .FirstOrDefaultAsync(t => t.Name == tagName, cancellationToken);

        return tag?.Captures ?? Enumerable.Empty<RawCapture>();
    }
}
