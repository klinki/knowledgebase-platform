using NpgsqlTypes;

namespace Sentinel.Application.Interfaces;

public interface IEmbeddingService
{
    Task<Vector> GenerateAsync(string input, CancellationToken cancellationToken);
}
