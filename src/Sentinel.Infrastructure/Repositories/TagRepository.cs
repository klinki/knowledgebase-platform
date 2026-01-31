using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Interfaces;
using Sentinel.Domain.Entities;
using Sentinel.Domain.Normalization;
using Sentinel.Infrastructure.Data;

namespace Sentinel.Infrastructure.Repositories;

public sealed class TagRepository : ITagRepository
{
    private readonly SentinelDbContext _dbContext;

    public TagRepository(SentinelDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Tag>> GetOrCreateAsync(IEnumerable<string> tagNames, CancellationToken cancellationToken)
    {
        var normalized = tagNames
            .Select(TagNormalizer.Normalize)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct()
            .ToList();

        if (normalized.Count == 0)
        {
            return Array.Empty<Tag>();
        }

        var existing = await _dbContext.Tags
            .Where(tag => normalized.Contains(tag.Name))
            .ToListAsync(cancellationToken);

        var missing = normalized
            .Except(existing.Select(tag => tag.Name))
            .Select(name => new Tag
            {
                Id = Guid.NewGuid(),
                Name = name
            })
            .ToList();

        if (missing.Count > 0)
        {
            _dbContext.Tags.AddRange(missing);
        }

        return existing.Concat(missing).ToList();
    }
}
