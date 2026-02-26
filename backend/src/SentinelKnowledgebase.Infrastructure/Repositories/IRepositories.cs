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
    Task<IEnumerable<SemanticSearchRecord>> SemanticSearchAsync(float[] queryEmbedding, int topK, double threshold);
    Task<IEnumerable<TagSearchRecord>> SearchByTagsAsync(IReadOnlyCollection<string> tags, bool matchAll);
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
