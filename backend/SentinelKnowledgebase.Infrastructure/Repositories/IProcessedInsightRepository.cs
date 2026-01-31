using Pgvector;
using SentinelKnowledgebase.Domain.Entities;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public interface IProcessedInsightRepository
{
    Task<ProcessedInsight?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProcessedInsight?> GetByRawCaptureIdAsync(Guid rawCaptureId, CancellationToken cancellationToken = default);
    Task<ProcessedInsight> AddAsync(ProcessedInsight insight, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProcessedInsight>> SearchByEmbeddingAsync(float[] embedding, int limit = 10, CancellationToken cancellationToken = default);
}
