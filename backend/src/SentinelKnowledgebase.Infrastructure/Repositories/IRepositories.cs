using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public interface IRawCaptureRepository
{
    Task<RawCapture> AddAsync(RawCapture rawCapture);
    Task<RawCapture?> GetByIdAsync(Guid id);
    Task<RawCapture?> GetByIdAsync(Guid id, Guid ownerUserId);
    Task<CaptureListQueryResult> GetPagedListAsync(Guid ownerUserId, CaptureListQueryOptions options);
    Task<IEnumerable<RawCapture>> GetAllAsync(Guid ownerUserId);
    Task<IEnumerable<RawCapture>> GetRecentAsync(Guid ownerUserId, int take);
    Task<IEnumerable<RawCapture>> GetRecentGlobalAsync(int take);
    Task<IReadOnlyDictionary<CaptureStatus, int>> GetStatusCountsAsync();
    Task<IReadOnlyList<Guid>> GetPendingIdsAsync();
    Task<int> CountAsync(Guid ownerUserId);
    Task UpdateAsync(RawCapture rawCapture);
    Task DeleteAsync(Guid id, Guid ownerUserId);
}

public class CaptureListQueryOptions
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string SortField { get; set; } = "createdAt";
    public string SortDirection { get; set; } = "desc";
    public ContentType? ContentType { get; set; }
    public CaptureStatus? Status { get; set; }
}

public class CaptureListQueryResult
{
    public List<CaptureListRecord> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

public class CaptureListRecord
{
    public Guid Id { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public ContentType ContentType { get; set; }
    public CaptureStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Metadata { get; set; }
}

public interface ICaptureProcessingControlRepository
{
    Task<CaptureProcessingControl?> GetAsync();
    Task<CaptureProcessingControl> AddAsync(CaptureProcessingControl control);
    Task UpdateAsync(CaptureProcessingControl control);
    Task<string?> GetDisplayNameAsync(Guid userId);
}

public interface IProcessedInsightRepository
{
    Task<ProcessedInsight> AddAsync(ProcessedInsight processedInsight);
    Task<ProcessedInsight?> GetByIdAsync(Guid id);
    Task<IEnumerable<ProcessedInsight>> GetAllAsync();
    Task<IEnumerable<SemanticSearchRecord>> SemanticSearchAsync(Guid ownerUserId, float[] queryEmbedding, int topK, double threshold);
    Task<IEnumerable<TagSearchRecord>> SearchByTagsAsync(Guid ownerUserId, IReadOnlyCollection<string> tags, bool matchAll);
    Task<IEnumerable<LabelSearchRecord>> SearchByLabelsAsync(Guid ownerUserId, IReadOnlyCollection<LabelRecord> labels, bool matchAll);
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
    public List<LabelRecord> Labels { get; set; } = new();
}

public class TagSearchRecord
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<LabelRecord> Labels { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
}

public class LabelSearchRecord
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<LabelRecord> Labels { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
}

public class LabelRecord
{
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
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
    Task<IEnumerable<TagSummaryRecord>> GetSummariesAsync(Guid ownerUserId, int? take = null, bool includeZeroCount = false);
    Task<int> CountAsync(Guid ownerUserId);
    Task UpdateAsync(Tag tag);
    Task DeleteAsync(Guid id);
}

public interface ILabelCategoryRepository
{
    Task<LabelCategory> AddAsync(LabelCategory category);
    Task<LabelCategory?> GetByIdAsync(Guid id);
    Task<LabelCategory?> GetByIdWithValuesAsync(Guid id);
    Task<LabelCategory?> GetByNameAsync(Guid ownerUserId, string name);
    Task<IEnumerable<LabelCategory>> GetAllWithValuesAsync(Guid ownerUserId);
    Task UpdateAsync(LabelCategory category);
    Task DeleteAsync(Guid id);
}

public interface ILabelValueRepository
{
    Task<LabelValue> AddAsync(LabelValue value);
    Task<LabelValue?> GetByIdAsync(Guid id);
    Task<LabelValue?> GetByCategoryAndValueAsync(Guid categoryId, string value);
    Task UpdateAsync(LabelValue value);
    Task DeleteAsync(Guid id);
}

public interface IEmbeddingVectorRepository
{
    Task<EmbeddingVector> AddAsync(EmbeddingVector embeddingVector);
    Task<EmbeddingVector?> GetByProcessedInsightIdAsync(Guid processedInsightId);
    Task UpdateAsync(EmbeddingVector embeddingVector);
    Task DeleteAsync(Guid id);
}
