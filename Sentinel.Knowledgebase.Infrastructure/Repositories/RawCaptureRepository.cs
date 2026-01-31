using Microsoft.EntityFrameworkCore;
using Sentinel.Knowledgebase.Domain.Entities;
using Sentinel.Knowledgebase.Domain.Enums;
using Sentinel.Knowledgebase.Infrastructure.Data;

namespace Sentinel.Knowledgebase.Infrastructure.Repositories;

public class RawCaptureRepository : Repository<RawCapture>, IRawCaptureRepository
{
    public RawCaptureRepository(SentinelDbContext context) : base(context)
    {
    }

    public async Task<RawCapture?> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(rc => rc.SourceUrl == url, cancellationToken);
    }

    public async Task<IEnumerable<RawCapture>> GetByStatusAsync(CaptureStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rc => rc.Status == status)
            .OrderByDescending(rc => rc.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<RawCapture>> GetPendingCapturesAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rc => rc.Status == CaptureStatus.Pending)
            .OrderBy(rc => rc.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
