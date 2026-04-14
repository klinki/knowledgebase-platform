using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;

namespace SentinelKnowledgebase.Infrastructure.Repositories;

public interface IRawCaptureRepository
{
    Task<RawCapture> AddAsync(RawCapture rawCapture);
    Task<RawCapture?> GetByIdAsync(Guid id);
    Task<RawCapture?> GetByIdAsync(Guid id, Guid ownerUserId);
    Task<IReadOnlyList<RawCapture>> GetByIdsAsync(Guid ownerUserId, IReadOnlyCollection<Guid> ids);
    Task<IReadOnlyList<RawCapture>> GetByIdsWithGraphAsync(Guid ownerUserId, IReadOnlyCollection<Guid> ids);
    Task<IReadOnlyList<CaptureMetadataRecord>> GetCompletedTweetsWithMetadataAsync(Guid ownerUserId, int take);
    Task<CaptureSearchQueryResult> SearchCapturesAsync(Guid ownerUserId, CaptureSearchQueryOptions options);
    Task<IReadOnlyList<RawCapture>> GetFailedAsync(Guid ownerUserId, ContentType? contentType = null);
    Task<CaptureListQueryResult> GetPagedListAsync(Guid ownerUserId, CaptureListQueryOptions options);
    Task<IEnumerable<RawCapture>> GetAllAsync(Guid ownerUserId);
    Task<IEnumerable<RawCapture>> GetRecentAsync(Guid ownerUserId, int take);
    Task<IEnumerable<RawCapture>> GetRecentGlobalAsync(int take);
    Task<IReadOnlyDictionary<CaptureStatus, int>> GetStatusCountsAsync();
    Task<IReadOnlyList<Guid>> GetPendingIdsAsync();
    Task<int> CountAsync(Guid ownerUserId);
    Task UpdateAsync(RawCapture rawCapture);
    Task DeleteAsync(Guid id, Guid ownerUserId);
    Task<int> DeleteByIdsAsync(Guid ownerUserId, IReadOnlyCollection<Guid> ids);
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

public class CaptureMetadataRecord
{
    public Guid Id { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string RawContent { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public class CaptureSearchQueryOptions
{
    public string? Query { get; set; }
    public float[]? QueryEmbedding { get; set; }
    public double Threshold { get; set; } = 0.3;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int MaxResultSetSize { get; set; } = 5000;
    public ContentType? ContentType { get; set; }
    public CaptureStatus? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public IReadOnlyCollection<string> Tags { get; set; } = [];
    public bool MatchAllTags { get; set; }
    public IReadOnlyCollection<LabelRecord> Labels { get; set; } = [];
    public bool MatchAllLabels { get; set; }
}

public class CaptureSearchQueryResult
{
    public List<Guid> CaptureIds { get; set; } = new();
    public List<CaptureSearchRecord> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class CaptureSearchRecord
{
    public Guid CaptureId { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string RawContent { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public ContentType ContentType { get; set; }
    public CaptureStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public double? Similarity { get; set; }
    public bool MatchedByText { get; set; }
    public bool MatchedBySemantic { get; set; }
}

public interface IAssistantChatRepository
{
    Task<AssistantChatSession> GetOrCreateSessionAsync(Guid ownerUserId);
    Task<AssistantChatSession?> GetByOwnerAsync(Guid ownerUserId);
    Task<IReadOnlyList<AssistantChatMessage>> GetMessagesAsync(Guid ownerUserId);
    Task<AssistantChatResultSet?> GetResultSetByIdAsync(Guid ownerUserId, Guid resultSetId);
    Task<IReadOnlyDictionary<Guid, AssistantChatResultSet>> GetResultSetsByIdsAsync(
        Guid ownerUserId,
        IReadOnlyCollection<Guid> resultSetIds);
    Task<AssistantChatResultSet?> GetLatestResultSetAsync(Guid ownerUserId);
    Task<AssistantChatPendingAction?> GetPendingActionAsync(Guid ownerUserId, Guid actionId);
    Task<IReadOnlyDictionary<Guid, AssistantChatPendingAction>> GetPendingActionsByIdsAsync(
        Guid ownerUserId,
        IReadOnlyCollection<Guid> actionIds);
    Task<AssistantChatPendingAction?> GetLatestPendingActionAsync(Guid ownerUserId);
    Task AddMessageAsync(AssistantChatMessage message);
    Task AddResultSetAsync(AssistantChatResultSet resultSet);
    Task AddPendingActionAsync(AssistantChatPendingAction action);
    Task UpdateSessionAsync(AssistantChatSession session);
    Task UpdatePendingActionAsync(AssistantChatPendingAction action);
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
    Task<SearchQueryResult> SearchAsync(
        Guid ownerUserId,
        float[]? queryEmbedding,
        double threshold,
        int page,
        int pageSize,
        IReadOnlyCollection<string> tags,
        bool matchAllTags,
        IReadOnlyCollection<LabelRecord> labels,
        bool matchAllLabels);
    Task<IReadOnlyList<ProcessedInsightEmbeddingRecord>> GetEmbeddingRecordsAsync(Guid ownerUserId);
    Task<IEnumerable<SemanticSearchRecord>> SemanticSearchAsync(Guid ownerUserId, float[] queryEmbedding, int topK, double threshold);
    Task<IEnumerable<TagSearchRecord>> SearchByTagsAsync(Guid ownerUserId, IReadOnlyCollection<string> tags, bool matchAll);
    Task<IEnumerable<LabelSearchRecord>> SearchByLabelsAsync(Guid ownerUserId, IReadOnlyCollection<LabelRecord> labels, bool matchAll);
    Task UpdateAsync(ProcessedInsight processedInsight);
    Task DeleteAsync(Guid id);
}

public class SearchRecord
{
    public Guid Id { get; set; }
    public Guid CaptureId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public double? Similarity { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<LabelRecord> Labels { get; set; } = new();
}

public class SearchQueryResult
{
    public List<SearchRecord> Items { get; set; } = new();
    public int TotalCount { get; set; }
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

public class ProcessedInsightEmbeddingRecord
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = [];
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

public interface IInsightClusterRepository
{
    Task<InsightCluster> AddAsync(InsightCluster cluster);
    Task AddMembershipsAsync(IEnumerable<InsightClusterMembership> memberships);
    Task<InsightCluster?> GetByIdAsync(Guid ownerUserId, Guid clusterId);
    Task<IReadOnlyList<InsightCluster>> GetTopAsync(Guid ownerUserId, int take);
    Task<TopicClusterQueryResult> GetPagedAsync(Guid ownerUserId, TopicClusterQueryOptions options);
    Task<IReadOnlyList<Guid>> GetStaleOwnerIdsAsync(DateTime staleBefore, int take);
    Task DeleteByOwnerAsync(Guid ownerUserId);
}

public class TopicClusterQueryOptions
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string? Query { get; set; }
    public string SortField { get; set; } = "memberCount";
    public string SortDirection { get; set; } = "desc";
}

public class TopicClusterQueryResult
{
    public List<InsightCluster> Items { get; set; } = new();
    public int TotalCount { get; set; }
}
