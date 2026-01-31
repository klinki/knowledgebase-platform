using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public interface IRawCaptureRepository
{
    Task<RawCapture?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RawCapture> AddAsync(RawCapture capture, CancellationToken cancellationToken = default);
    Task UpdateAsync(RawCapture capture, CancellationToken cancellationToken = default);
    Task<IEnumerable<RawCapture>> GetPendingAsync(CancellationToken cancellationToken = default);
}
