using SentinelKnowledgebase.Domain.Entities;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public interface IRawCaptureRepository
{
    Task<RawCapture> AddAsync(RawCapture rawCapture);
    Task<RawCapture?> GetByIdAsync(Guid id);
    Task<RawCapture?> GetByIdAsync(Guid id, Guid ownerUserId);
    Task<IEnumerable<RawCapture>> GetAllAsync(Guid ownerUserId);
    Task<IEnumerable<RawCapture>> GetRecentAsync(Guid ownerUserId, int take);
    Task<int> CountAsync(Guid ownerUserId);
    Task UpdateAsync(RawCapture rawCapture);
    Task DeleteAsync(Guid id, Guid ownerUserId);
}

public interface IProcessedInsightRepository
{
    Task<ProcessedInsight> AddAsync(ProcessedInsight processedInsight);
    Task<ProcessedInsight?> GetByIdAsync(Guid id);
    Task<IEnumerable<ProcessedInsight>> GetAllAsync();
    Task<IEnumerable<SemanticSearchRecord>> SemanticSearchAsync(Guid ownerUserId, float[] queryEmbedding, int topK, double threshold);
    Task<IEnumerable<TagSearchRecord>> SearchByTagsAsync(Guid ownerUserId, IReadOnlyCollection<string> tags, bool matchAll);
    Task UpdateAsync(ProcessedInsight processedInsight);
    Task DeleteAsync(Guid id);
}

public class SemanticSearchRecord
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public double Similarity { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class TagSearchRecord
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
}

public class TagSummaryRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public interface ITagRepository
{
    Task<Tag> AddAsync(Tag tag);
    Task<Tag?> GetByIdAsync(Guid id);
    Task<Tag?> GetByNameAsync(Guid ownerUserId, string name);
    Task<IEnumerable<Tag>> GetAllAsync(Guid ownerUserId);
    Task<IEnumerable<TagSummaryRecord>> GetSummariesAsync(Guid ownerUserId, int? take = null);
    Task<int> CountAsync(Guid ownerUserId);
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
