using Sentinel.Domain.Entities;

namespace Sentinel.Application.Interfaces;

public interface ITagRepository
{
    Task<IReadOnlyCollection<Tag>> GetOrCreateAsync(IEnumerable<string> tagNames, CancellationToken cancellationToken);
}
