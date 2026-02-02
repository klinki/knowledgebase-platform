using SentinelKnowledgebase.Domain.Entities;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public interface IRawCaptureRepository
{
    Task<RawCapture> AddAsync(RawCapture rawCapture);
    Task<RawCapture?> GetByIdAsync(Guid id);
    Task<IEnumerable<RawCapture>> GetAllAsync();
    Task UpdateAsync(RawCapture rawCapture);
    Task DeleteAsync(Guid id);
}

public interface IProcessedInsightRepository
{
    Task<ProcessedInsight> AddAsync(ProcessedInsight processedInsight);
    Task<ProcessedInsight?> GetByIdAsync(Guid id);
    Task<IEnumerable<ProcessedInsight>> GetAllAsync();
    Task UpdateAsync(ProcessedInsight processedInsight);
    Task DeleteAsync(Guid id);
}

public interface ITagRepository
{
    Task<Tag> AddAsync(Tag tag);
    Task<Tag?> GetByIdAsync(Guid id);
    Task<Tag?> GetByNameAsync(string name);
    Task<IEnumerable<Tag>> GetAllAsync();
    Task UpdateAsync(Tag tag);
    Task DeleteAsync(Guid id);
}

public interface IEmbeddingVectorRepository
{
    Task<EmbeddingVector> AddAsync(EmbeddingVector embeddingVector);
    Task<EmbeddingVector?> GetByProcessedInsightIdAsync(Guid processedInsightId);
    Task UpdateAsync(EmbeddingVector embeddingVector);
    Task DeleteAsync(Guid id);
}
