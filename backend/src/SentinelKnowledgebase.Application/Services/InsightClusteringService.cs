using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SentinelKnowledgebase.Application.DTOs.Clusters;
using SentinelKnowledgebase.Application.DTOs.Labels;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Application.Services;

public class InsightClusteringService : IInsightClusteringService
{
    private const int MinimumInsightCount = 6;
    private const int MinimumClusterSize = 3;
    private const int NeighborLimit = 5;
    private const int SummarySampleSize = 5;
    private const double SimilarityThreshold = 0.72d;
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> OwnerLocks = new();
    private const string DefaultSortField = "memberCount";
    private const string DefaultSortDirection = "desc";
    private static readonly HashSet<string> StopWords =
    [
        "about", "after", "before", "between", "could", "from", "have", "into", "just", "like",
        "more", "most", "only", "over", "such", "than", "that", "their", "them", "there", "these",
        "they", "this", "what", "when", "where", "which", "while", "with", "would", "your"
    ];

    private readonly IUnitOfWork _unitOfWork;
    private readonly IContentProcessor _contentProcessor;
    private readonly ILogger<InsightClusteringService> _logger;

    public InsightClusteringService(
        IUnitOfWork unitOfWork,
        IContentProcessor contentProcessor,
        ILogger<InsightClusteringService> logger)
    {
        _unitOfWork = unitOfWork;
        _contentProcessor = contentProcessor;
        _logger = logger;
    }

    public async Task RebuildOwnerClustersAsync(Guid ownerUserId)
    {
        var ownerLock = OwnerLocks.GetOrAdd(ownerUserId, _ => new SemaphoreSlim(1, 1));
        if (!await ownerLock.WaitAsync(0))
        {
            _logger.LogInformation("Cluster rebuild skipped for owner {OwnerUserId} because another rebuild is already running.", ownerUserId);
            return;
        }

        try
        {
            var records = await _unitOfWork.ProcessedInsights.GetEmbeddingRecordsAsync(ownerUserId);
            if (records.Count < MinimumInsightCount)
            {
                await _unitOfWork.InsightClusters.DeleteByOwnerAsync(ownerUserId);
                await _unitOfWork.SaveChangesAsync();
                return;
            }

            var components = BuildClusterComponents(records);
            if (components.Count == 0)
            {
                await _unitOfWork.InsightClusters.DeleteByOwnerAsync(ownerUserId);
                await _unitOfWork.SaveChangesAsync();
                return;
            }

            var now = DateTime.UtcNow;
            var clusters = new List<InsightCluster>(components.Count);
            var memberships = new List<InsightClusterMembership>();

            foreach (var component in components)
            {
                var rankedMembers = RankMembersByCentroid(component);
                var metadata = await GenerateMetadataAsync(rankedMembers);
                var clusterId = Guid.NewGuid();

                clusters.Add(new InsightCluster
                {
                    Id = clusterId,
                    OwnerUserId = ownerUserId,
                    Title = metadata.Title,
                    Description = metadata.Description,
                    KeywordsJson = JsonSerializer.Serialize(metadata.Keywords),
                    MemberCount = rankedMembers.Count,
                    RepresentativeProcessedInsightId = rankedMembers[0].Record.Id,
                    LastComputedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                });

                for (var index = 0; index < rankedMembers.Count; index++)
                {
                    memberships.Add(new InsightClusterMembership
                    {
                        InsightClusterId = clusterId,
                        ProcessedInsightId = rankedMembers[index].Record.Id,
                        Rank = index + 1,
                        SimilarityToCentroid = rankedMembers[index].SimilarityToCentroid,
                        CreatedAt = now
                    });
                }
            }

            await _unitOfWork.InsightClusters.DeleteByOwnerAsync(ownerUserId);
            foreach (var cluster in clusters)
            {
                await _unitOfWork.InsightClusters.AddAsync(cluster);
            }

            await _unitOfWork.InsightClusters.AddMembershipsAsync(memberships);
            await _unitOfWork.SaveChangesAsync();
        }
        finally
        {
            ownerLock.Release();
        }
    }

    public async Task RebuildStaleOwnerClustersAsync()
    {
        var ownerIds = await _unitOfWork.InsightClusters.GetStaleOwnerIdsAsync(DateTime.UtcNow.AddHours(-24), 250);
        foreach (var ownerId in ownerIds)
        {
            await RebuildOwnerClustersAsync(ownerId);
        }
    }

    public async Task<IReadOnlyList<TopicClusterSummaryDto>> GetClusterSummariesAsync(Guid ownerUserId, int take = 5)
    {
        var clusters = await _unitOfWork.InsightClusters.GetTopAsync(ownerUserId, take);
        return clusters.Select(MapSummary).ToList();
    }

    public async Task<TopicClusterListPageDto> GetClusterListPageAsync(Guid ownerUserId, TopicClusterListQueryDto query)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var normalizedQuery = string.IsNullOrWhiteSpace(query.Query) ? null : query.Query.Trim();
        var sortField = NormalizeSortField(query.SortField);
        var sortDirection = NormalizeSortDirection(query.SortDirection);
        var result = await _unitOfWork.InsightClusters.GetPagedAsync(ownerUserId, new TopicClusterQueryOptions
        {
            Page = page,
            PageSize = pageSize,
            Query = normalizedQuery,
            SortField = sortField,
            SortDirection = sortDirection
        });

        return new TopicClusterListPageDto
        {
            Items = result.Items.Select(MapSummary).ToList(),
            TotalCount = result.TotalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private static string NormalizeSortField(string? sortField)
    {
        return sortField?.Trim() switch
        {
            "memberCount" => "memberCount",
            "updatedAt" => "updatedAt",
            "title" => "title",
            _ => DefaultSortField
        };
    }

    private static string NormalizeSortDirection(string? sortDirection)
    {
        return sortDirection?.Trim().ToLowerInvariant() switch
        {
            "asc" => "asc",
            "desc" => "desc",
            _ => DefaultSortDirection
        };
    }

    public async Task<TopicClusterDetailDto?> GetClusterDetailAsync(Guid ownerUserId, Guid clusterId)
    {
        var cluster = await _unitOfWork.InsightClusters.GetByIdAsync(ownerUserId, clusterId);
        return cluster == null ? null : MapDetail(cluster);
    }

    private async Task<ClusterMetadata> GenerateMetadataAsync(IReadOnlyList<RankedClusterMember> rankedMembers)
    {
        var summaries = rankedMembers
            .Take(SummarySampleSize)
            .Select(member => member.Record.Summary)
            .Where(summary => !string.IsNullOrWhiteSpace(summary))
            .ToList();

        var fallbackKeywords = ExtractFallbackKeywords(summaries);

        try
        {
            var generated = await _contentProcessor.GenerateClusterMetadataAsync(summaries);
            var title = generated.Title.Trim();
            if (title.Length == 0)
            {
                title = string.Join(" / ", fallbackKeywords.Take(2));
            }

            if (title.Length == 0)
            {
                title = rankedMembers[0].Record.Title;
            }

            return new ClusterMetadata
            {
                Title = TrimToLength(title, 60),
                Description = string.IsNullOrWhiteSpace(generated.Description) ? null : TrimToLength(generated.Description.Trim(), 160),
                Keywords = generated.Keywords
                    .Select(keyword => keyword.Trim())
                    .Where(keyword => keyword.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(3)
                    .DefaultIfEmpty(fallbackKeywords.FirstOrDefault() ?? rankedMembers[0].Record.Title)
                    .ToList()
            };
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Cluster title generation failed. Falling back to local keyword extraction.");
            var title = fallbackKeywords.Count > 0
                ? string.Join(" / ", fallbackKeywords.Take(2))
                : rankedMembers[0].Record.Title;

            return new ClusterMetadata
            {
                Title = TrimToLength(title, 60),
                Description = null,
                Keywords = fallbackKeywords.Take(3).ToList()
            };
        }
    }

    private static List<HashSet<ProcessedInsightEmbeddingRecord>> BuildClusterComponents(IReadOnlyList<ProcessedInsightEmbeddingRecord> records)
    {
        var neighbors = records.ToDictionary(record => record.Id, _ => new List<(Guid Id, double Similarity)>());

        for (var index = 0; index < records.Count; index++)
        {
            var current = new List<(Guid Id, double Similarity)>();
            for (var candidateIndex = 0; candidateIndex < records.Count; candidateIndex++)
            {
                if (index == candidateIndex)
                {
                    continue;
                }

                var similarity = CosineSimilarity(records[index].Embedding, records[candidateIndex].Embedding);
                if (similarity < SimilarityThreshold)
                {
                    continue;
                }

                current.Add((records[candidateIndex].Id, similarity));
            }

            neighbors[records[index].Id] = current
                .OrderByDescending(item => item.Similarity)
                .ThenBy(item => item.Id)
                .Take(NeighborLimit)
                .ToList();
        }

        var adjacency = records.ToDictionary(record => record.Id, _ => new HashSet<Guid>());
        foreach (var record in records)
        {
            foreach (var neighbor in neighbors[record.Id])
            {
                if (neighbors.TryGetValue(neighbor.Id, out var neighborList) &&
                    neighborList.Any(candidate => candidate.Id == record.Id))
                {
                    adjacency[record.Id].Add(neighbor.Id);
                    adjacency[neighbor.Id].Add(record.Id);
                }
            }
        }

        var recordLookup = records.ToDictionary(record => record.Id);
        var visited = new HashSet<Guid>();
        var components = new List<HashSet<ProcessedInsightEmbeddingRecord>>();

        foreach (var record in records)
        {
            if (!visited.Add(record.Id))
            {
                continue;
            }

            var component = new HashSet<ProcessedInsightEmbeddingRecord>();
            var pending = new Stack<Guid>();
            pending.Push(record.Id);

            while (pending.Count > 0)
            {
                var currentId = pending.Pop();
                component.Add(recordLookup[currentId]);

                foreach (var adjacentId in adjacency[currentId])
                {
                    if (visited.Add(adjacentId))
                    {
                        pending.Push(adjacentId);
                    }
                }
            }

            if (component.Count >= MinimumClusterSize)
            {
                components.Add(component);
            }
        }

        return components;
    }

    private static List<RankedClusterMember> RankMembersByCentroid(HashSet<ProcessedInsightEmbeddingRecord> component)
    {
        var records = component.ToList();
        var dimensions = records[0].Embedding.Length;
        var centroid = new double[dimensions];

        foreach (var record in records)
        {
            for (var index = 0; index < dimensions; index++)
            {
                centroid[index] += record.Embedding[index];
            }
        }

        var magnitude = Math.Sqrt(centroid.Sum(value => value * value));
        if (magnitude > 0)
        {
            for (var index = 0; index < dimensions; index++)
            {
                centroid[index] /= magnitude;
            }
        }

        return records
            .Select(record => new RankedClusterMember(record, CosineSimilarity(record.Embedding, centroid)))
            .OrderByDescending(member => member.SimilarityToCentroid)
            .ThenBy(member => member.Record.Id)
            .ToList();
    }

    private static double CosineSimilarity(IReadOnlyList<float> left, IReadOnlyList<float> right)
    {
        double dot = 0;
        double leftNorm = 0;
        double rightNorm = 0;

        for (var index = 0; index < left.Count; index++)
        {
            dot += left[index] * right[index];
            leftNorm += left[index] * left[index];
            rightNorm += right[index] * right[index];
        }

        if (leftNorm == 0 || rightNorm == 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm));
    }

    private static double CosineSimilarity(IReadOnlyList<float> left, IReadOnlyList<double> right)
    {
        double dot = 0;
        double leftNorm = 0;
        double rightNorm = 0;

        for (var index = 0; index < left.Count; index++)
        {
            dot += left[index] * right[index];
            leftNorm += left[index] * left[index];
            rightNorm += right[index] * right[index];
        }

        if (leftNorm == 0 || rightNorm == 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm));
    }

    private static List<string> ExtractFallbackKeywords(IEnumerable<string> summaries)
    {
        return summaries
            .SelectMany(summary => summary
                .Split([' ', '\r', '\n', '\t', ',', '.', ';', ':', '!', '?', '(', ')', '[', ']', '"', '\''], StringSplitOptions.RemoveEmptyEntries)
                .Select(token => token.Trim().ToLowerInvariant()))
            .Where(token => token.Length >= 4 && !StopWords.Contains(token))
            .GroupBy(token => token)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => group.Key)
            .Take(3)
            .ToList();
    }

    private static string TrimToLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength].TrimEnd();
    }

    private static TopicClusterSummaryDto MapSummary(InsightCluster cluster)
    {
        return new TopicClusterSummaryDto
        {
            Id = cluster.Id,
            Title = cluster.Title,
            Description = cluster.Description,
            Keywords = ReadKeywords(cluster.KeywordsJson),
            MemberCount = cluster.MemberCount,
            UpdatedAt = cluster.UpdatedAt,
            SuggestedLabel = BuildSuggestedLabel(cluster.Title),
            RepresentativeInsights = cluster.Memberships
                .OrderBy(membership => membership.Rank)
                .Take(3)
                .Select(membership => new TopicClusterRepresentativeInsightDto
                {
                    CaptureId = membership.ProcessedInsight.RawCaptureId,
                    ProcessedInsightId = membership.ProcessedInsightId,
                    Title = membership.ProcessedInsight.Title,
                    Summary = membership.ProcessedInsight.Summary,
                    SourceUrl = membership.ProcessedInsight.RawCapture.SourceUrl
                })
                .ToList()
        };
    }

    private static TopicClusterDetailDto MapDetail(InsightCluster cluster)
    {
        return new TopicClusterDetailDto
        {
            Id = cluster.Id,
            Title = cluster.Title,
            Description = cluster.Description,
            Keywords = ReadKeywords(cluster.KeywordsJson),
            MemberCount = cluster.MemberCount,
            UpdatedAt = cluster.UpdatedAt,
            SuggestedLabel = BuildSuggestedLabel(cluster.Title),
            Members = cluster.Memberships
                .OrderBy(membership => membership.Rank)
                .Select(membership => new TopicClusterMembershipDto
                {
                    CaptureId = membership.ProcessedInsight.RawCaptureId,
                    ProcessedInsightId = membership.ProcessedInsightId,
                    Title = membership.ProcessedInsight.Title,
                    Summary = membership.ProcessedInsight.Summary,
                    SourceUrl = membership.ProcessedInsight.RawCapture.SourceUrl,
                    Rank = membership.Rank,
                    SimilarityToCentroid = membership.SimilarityToCentroid,
                    Tags = membership.ProcessedInsight.Tags.Select(tag => tag.Name).ToList(),
                    Labels = membership.ProcessedInsight.LabelAssignments
                        .OrderBy(assignment => assignment.LabelCategory.Name)
                        .ThenBy(assignment => assignment.LabelValue.Value)
                        .Select(assignment => new LabelAssignmentDto
                        {
                            Category = assignment.LabelCategory.Name,
                            Value = assignment.LabelValue.Value
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private static LabelAssignmentDto BuildSuggestedLabel(string title)
    {
        return new LabelAssignmentDto
        {
            Category = "Topic",
            Value = title
        };
    }

    private static List<string> ReadKeywords(string keywordsJson)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(keywordsJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record RankedClusterMember(ProcessedInsightEmbeddingRecord Record, double SimilarityToCentroid);
}
