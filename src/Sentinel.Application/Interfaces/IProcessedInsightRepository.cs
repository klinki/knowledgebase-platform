using NpgsqlTypes;
using Sentinel.Domain.Entities;

namespace Sentinel.Application.Interfaces;

public interface IProcessedInsightRepository
{
    Task AddAsync(ProcessedInsight insight, CancellationToken cancellationToken);
    Task<ProcessedInsight?> GetByCaptureIdAsync(Guid rawCaptureId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ProcessedInsight>> SearchSemanticAsync(Vector embedding, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ProcessedInsight>> SearchByTagsAsync(IReadOnlyCollection<string> tags, bool matchAll, int limit, CancellationToken cancellationToken);
}
