using Sentinel.Application.Interfaces;
using Sentinel.Infrastructure.Data;

namespace Sentinel.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SentinelDbContext _dbContext;

    public UnitOfWork(SentinelDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
