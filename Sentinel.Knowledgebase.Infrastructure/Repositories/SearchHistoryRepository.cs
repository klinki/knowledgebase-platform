using Microsoft.EntityFrameworkCore;
using Sentinel.Knowledgebase.Domain.Entities;
using Sentinel.Knowledgebase.Infrastructure.Data;

namespace Sentinel.Knowledgebase.Infrastructure.Repositories;

public class SearchHistoryRepository : Repository<SearchHistory>, ISearchHistoryRepository
{
    public SearchHistoryRepository(SentinelDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SearchHistory>> GetByUserIdAsync(string userId, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sh => sh.UserId == userId)
            .OrderByDescending(sh => sh.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SearchHistory>> GetBySearchTypeAsync(string searchType, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(sh => sh.SearchType == searchType)
            .OrderByDescending(sh => sh.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
