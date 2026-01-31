using Sentinel.Domain.Entities;

namespace Sentinel.Application.Interfaces;

public interface IRawCaptureRepository
{
    Task AddAsync(RawCapture rawCapture, CancellationToken cancellationToken);
    Task<RawCapture?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
