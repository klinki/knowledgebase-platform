using Sentinel.Knowledgebase.Domain.Entities;

namespace Sentinel.Knowledgebase.Infrastructure.Repositories;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IRawCaptureRepository : IRepository<RawCapture>
{
    Task<RawCapture?> GetByUrlAsync(string url, CancellationToken cancellationToken = default);
    Task<IEnumerable<RawCapture>> GetByStatusAsync(Domain.Enums.CaptureStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<RawCapture>> GetPendingCapturesAsync(int limit = 50, CancellationToken cancellationToken = default);
}

public interface IProcessedInsightRepository : IRepository<ProcessedInsight>
{
    Task<ProcessedInsight?> GetByRawCaptureIdAsync(Guid rawCaptureId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProcessedInsight>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProcessedInsight>> GetByTagsAsync(List<string> tags, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProcessedInsight>> SemanticSearchAsync(float[] queryEmbedding, int limit = 10, float threshold = 0.7f, CancellationToken cancellationToken = default);
}

public interface ISearchHistoryRepository : IRepository<SearchHistory>
{
    Task<IEnumerable<SearchHistory>> GetByUserIdAsync(string userId, int limit = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<SearchHistory>> GetBySearchTypeAsync(string searchType, int limit = 50, CancellationToken cancellationToken = default);
}
