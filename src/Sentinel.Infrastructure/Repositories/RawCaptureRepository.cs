using Microsoft.EntityFrameworkCore;
using Sentinel.Application.Interfaces;
using Sentinel.Domain.Entities;
using Sentinel.Infrastructure.Data;

namespace Sentinel.Infrastructure.Repositories;

public sealed class RawCaptureRepository : IRawCaptureRepository
{
    private readonly SentinelDbContext _dbContext;

    public RawCaptureRepository(SentinelDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(RawCapture rawCapture, CancellationToken cancellationToken)
    {
        return _dbContext.RawCaptures.AddAsync(rawCapture, cancellationToken).AsTask();
    }

    public Task<RawCapture?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.RawCaptures
            .Include(capture => capture.ProcessedInsight)
            .ThenInclude(insight => insight!.Tags)
            .ThenInclude(link => link.Tag)
            .AsNoTracking()
            .FirstOrDefaultAsync(capture => capture.Id == id, cancellationToken);
    }
}
