using SentinelKnowledgebase.Domain.Entities;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public interface ITagRepository
{
    Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Tag> AddAsync(Tag tag, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tag>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<RawCapture>> GetCapturesByTagAsync(string tagName, CancellationToken cancellationToken = default);
}
