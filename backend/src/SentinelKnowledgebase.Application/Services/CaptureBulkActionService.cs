using System.Text.Json;
using SentinelKnowledgebase.Application.DTOs.Search;
using Microsoft.Extensions.Logging;
using SentinelKnowledgebase.Application.DTOs.Labels;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Application.Services;

public class CaptureBulkActionService : ICaptureBulkActionService
{
    private static readonly HashSet<string> DeletedTweetSkipCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "twitter_suspended_account",
        "twitter_account_limited",
        "twitter_post_unavailable"
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IContentProcessor _contentProcessor;
    private readonly ILogger<CaptureBulkActionService> _logger;

    public CaptureBulkActionService(
        IUnitOfWork unitOfWork,
        IContentProcessor contentProcessor,
        ILogger<CaptureBulkActionService> logger)
    {
        _unitOfWork = unitOfWork;
        _contentProcessor = contentProcessor;
        _logger = logger;
    }

    public async Task<CaptureBulkQueryResult> SearchCapturesAsync(
        Guid ownerUserId,
        CaptureSearchCriteria criteria,
        int maxResultSetSize,
        int previewSize)
    {
        var normalizedQuery = NormalizeQuery(criteria.Query);
        var normalizedTags = NormalizeTags(criteria.Tags).ToList();
        var normalizedLabels = NormalizeLabels(criteria.Labels)
            .Select(label => new LabelRecord
            {
                Category = label.Category,
                Value = label.Value
            })
            .ToList();

        if (!HasAtLeastOneSearchCriterion(
                normalizedQuery,
                normalizedTags,
                normalizedLabels,
                criteria.ContentType,
                criteria.Status,
                criteria.DateFrom,
                criteria.DateTo))
        {
            throw new ArgumentException("At least one search criterion is required.", nameof(criteria));
        }

        var page = criteria.Page > 0 ? criteria.Page : 1;
        var pageSize = Math.Clamp(criteria.PageSize > 0 ? criteria.PageSize : 20, 1, 100);
        var threshold = Math.Clamp(criteria.Threshold, 0, 1);
        var hasQuery = normalizedQuery is not null;
        var (sortField, sortDirection) = NormalizeSort(criteria.SortField, criteria.SortDirection, hasQuery);
        float[]? queryEmbedding = null;

        if (normalizedQuery is not null)
        {
            queryEmbedding = await _contentProcessor.GenerateEmbeddingAsync(normalizedQuery);
        }

        var result = await _unitOfWork.RawCaptures.SearchCapturesAsync(ownerUserId, new CaptureSearchQueryOptions
        {
            Query = normalizedQuery,
            QueryEmbedding = queryEmbedding,
            Threshold = threshold,
            Page = page,
            PageSize = pageSize,
            MaxResultSetSize = Math.Clamp(maxResultSetSize, 1, 5000),
            ContentType = criteria.ContentType,
            Status = criteria.Status,
            DateFrom = criteria.DateFrom,
            DateTo = criteria.DateTo,
            Tags = normalizedTags,
            MatchAllTags = SearchMatchModes.IsAll(criteria.TagMatchMode),
            Labels = normalizedLabels,
            MatchAllLabels = SearchMatchModes.IsAll(criteria.LabelMatchMode),
            SortField = sortField,
            SortDirection = sortDirection
        });

        var normalizedPreviewSize = Math.Clamp(previewSize, 1, 100);
        var normalizedCriteria = new CaptureSearchCriteria
        {
            Query = normalizedQuery,
            Tags = normalizedTags,
            TagMatchMode = SearchMatchModes.IsAll(criteria.TagMatchMode) ? SearchMatchModes.All : SearchMatchModes.Any,
            Labels = NormalizeLabels(criteria.Labels),
            LabelMatchMode = SearchMatchModes.IsAll(criteria.LabelMatchMode) ? SearchMatchModes.All : SearchMatchModes.Any,
            Page = page,
            PageSize = pageSize,
            Threshold = threshold,
            ContentType = criteria.ContentType,
            Status = criteria.Status,
            DateFrom = criteria.DateFrom,
            DateTo = criteria.DateTo,
            SortField = sortField,
            SortDirection = sortDirection
        };

        return new CaptureBulkQueryResult
        {
            CaptureIds = result.CaptureIds,
            PreviewItems = result.Items
                .Take(normalizedPreviewSize)
                .Select(MapSearchPreviewItem)
                .ToList(),
            TotalCount = result.TotalCount,
            Summary = BuildSearchSummary(
                result.TotalCount,
                result.Items.Count,
                result.Page,
                result.PageSize,
                result.CaptureIds.Count,
                normalizedQuery),
            NormalizedCriteria = normalizedCriteria
        };
    }

    public async Task<CaptureBulkQueryResult> FindDeletedTweetsFromUnavailableAccountsAsync(
        Guid ownerUserId,
        int maxResultSetSize,
        int previewSize)
    {
        var records = await _unitOfWork.RawCaptures.GetCompletedTweetsWithMetadataAsync(
            ownerUserId,
            Math.Clamp(maxResultSetSize, 1, 5000));

        var matches = new List<CaptureBulkPreviewItem>();
        foreach (var record in records)
        {
            if (!TryExtractSkipMetadata(record.Metadata, out var skipCode, out var skipReason))
            {
                continue;
            }

            if (!DeletedTweetSkipCodes.Contains(skipCode))
            {
                continue;
            }

            matches.Add(new CaptureBulkPreviewItem
            {
                CaptureId = record.Id,
                SourceUrl = record.SourceUrl,
                SkipCode = skipCode,
                SkipReason = skipReason,
                CreatedAt = record.CreatedAt
            });
        }

        var normalizedPreviewSize = Math.Clamp(previewSize, 1, 50);
        return new CaptureBulkQueryResult
        {
            CaptureIds = matches.Select(match => match.CaptureId).ToList(),
            PreviewItems = matches.Take(normalizedPreviewSize).ToList(),
            TotalCount = matches.Count,
            Summary = matches.Count == 1
                ? "Found 1 tweet from a deleted or unavailable account."
                : $"Found {matches.Count} tweets from deleted or unavailable accounts."
        };
    }

    public async Task<CaptureBulkMutationResult> AddTagsAsync(
        Guid ownerUserId,
        IReadOnlyCollection<Guid> captureIds,
        IReadOnlyCollection<string> tags)
    {
        var normalizedTags = NormalizeTags(tags);
        var captures = await _unitOfWork.RawCaptures.GetByIdsWithGraphAsync(ownerUserId, captureIds);
        if (captures.Count == 0 || normalizedTags.Count == 0)
        {
            return CreateMutationResult(captureIds.Count, captures.Count, 0);
        }

        var existingTags = (await _unitOfWork.Tags.GetAllAsync(ownerUserId))
            .ToDictionary(tag => tag.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var tagName in normalizedTags)
        {
            if (existingTags.ContainsKey(tagName))
            {
                continue;
            }

            var tag = new Tag
            {
                Id = Guid.NewGuid(),
                OwnerUserId = ownerUserId,
                Name = tagName,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Tags.AddAsync(tag);
            existingTags[tagName] = tag;
        }

        var mutatedCount = 0;
        foreach (var capture in captures)
        {
            var captureChanged = false;
            var insightChanged = false;

            foreach (var tagName in normalizedTags)
            {
                var tag = existingTags[tagName];
                if (capture.Tags.All(existing => existing.Id != tag.Id))
                {
                    capture.Tags.Add(tag);
                    captureChanged = true;
                }

                if (capture.ProcessedInsight != null &&
                    capture.ProcessedInsight.Tags.All(existing => existing.Id != tag.Id))
                {
                    capture.ProcessedInsight.Tags.Add(tag);
                    insightChanged = true;
                }
            }

            if (!captureChanged && !insightChanged)
            {
                continue;
            }

            mutatedCount++;
            await _unitOfWork.RawCaptures.UpdateAsync(capture);
            if (insightChanged && capture.ProcessedInsight != null)
            {
                await _unitOfWork.ProcessedInsights.UpdateAsync(capture.ProcessedInsight);
            }
        }

        if (mutatedCount > 0 || normalizedTags.Any(tag => !existingTags.ContainsKey(tag)))
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return CreateMutationResult(captureIds.Count, captures.Count, mutatedCount);
    }

    public async Task<CaptureBulkMutationResult> RemoveTagsAsync(
        Guid ownerUserId,
        IReadOnlyCollection<Guid> captureIds,
        IReadOnlyCollection<string> tags)
    {
        var normalizedTags = NormalizeTags(tags);
        var captures = await _unitOfWork.RawCaptures.GetByIdsWithGraphAsync(ownerUserId, captureIds);
        if (captures.Count == 0 || normalizedTags.Count == 0)
        {
            return CreateMutationResult(captureIds.Count, captures.Count, 0);
        }

        var mutatedCount = 0;
        foreach (var capture in captures)
        {
            var removedFromCapture = capture.Tags.RemoveAll(tag => normalizedTags.Contains(tag.Name)) > 0;
            var removedFromInsight = capture.ProcessedInsight != null &&
                                     capture.ProcessedInsight.Tags.RemoveAll(tag => normalizedTags.Contains(tag.Name)) > 0;

            if (!removedFromCapture && !removedFromInsight)
            {
                continue;
            }

            mutatedCount++;
            await _unitOfWork.RawCaptures.UpdateAsync(capture);
            if (removedFromInsight && capture.ProcessedInsight != null)
            {
                await _unitOfWork.ProcessedInsights.UpdateAsync(capture.ProcessedInsight);
            }
        }

        if (mutatedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return CreateMutationResult(captureIds.Count, captures.Count, mutatedCount);
    }

    public async Task<CaptureBulkMutationResult> AddLabelsAsync(
        Guid ownerUserId,
        IReadOnlyCollection<Guid> captureIds,
        IReadOnlyCollection<LabelAssignmentDto> labels)
    {
        var normalizedLabels = NormalizeLabels(labels);
        var captures = await _unitOfWork.RawCaptures.GetByIdsWithGraphAsync(ownerUserId, captureIds);
        if (captures.Count == 0 || normalizedLabels.Count == 0)
        {
            return CreateMutationResult(captureIds.Count, captures.Count, 0);
        }

        var categories = (await _unitOfWork.LabelCategories.GetAllWithValuesAsync(ownerUserId)).ToList();
        var categoriesByName = categories.ToDictionary(category => category.Name, StringComparer.OrdinalIgnoreCase);
        var valuesByCategory = categories.ToDictionary(
            category => category.Id,
            category => category.Values.ToDictionary(value => value.Value, StringComparer.OrdinalIgnoreCase));

        foreach (var label in normalizedLabels)
        {
            if (!categoriesByName.TryGetValue(label.Category, out var category))
            {
                category = new LabelCategory
                {
                    Id = Guid.NewGuid(),
                    OwnerUserId = ownerUserId,
                    Name = label.Category,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.LabelCategories.AddAsync(category);
                categoriesByName[label.Category] = category;
                valuesByCategory[category.Id] = new Dictionary<string, LabelValue>(StringComparer.OrdinalIgnoreCase);
            }

            if (!valuesByCategory.TryGetValue(category.Id, out var valueLookup))
            {
                valueLookup = new Dictionary<string, LabelValue>(StringComparer.OrdinalIgnoreCase);
                valuesByCategory[category.Id] = valueLookup;
            }

            if (valueLookup.ContainsKey(label.Value))
            {
                continue;
            }

            var value = new LabelValue
            {
                Id = Guid.NewGuid(),
                LabelCategoryId = category.Id,
                LabelCategory = category,
                Value = label.Value,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.LabelValues.AddAsync(value);
            valueLookup[label.Value] = value;
        }

        var mutatedCount = 0;
        foreach (var capture in captures)
        {
            var captureChanged = false;
            var insightChanged = false;

            foreach (var label in normalizedLabels)
            {
                var category = categoriesByName[label.Category];
                var value = valuesByCategory[category.Id][label.Value];
                captureChanged |= UpsertRawCaptureLabel(capture, category, value);
                if (capture.ProcessedInsight != null)
                {
                    insightChanged |= UpsertProcessedInsightLabel(capture.ProcessedInsight, category, value);
                }
            }

            if (!captureChanged && !insightChanged)
            {
                continue;
            }

            mutatedCount++;
            await _unitOfWork.RawCaptures.UpdateAsync(capture);
            if (insightChanged && capture.ProcessedInsight != null)
            {
                await _unitOfWork.ProcessedInsights.UpdateAsync(capture.ProcessedInsight);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return CreateMutationResult(captureIds.Count, captures.Count, mutatedCount);
    }

    public async Task<CaptureBulkMutationResult> RemoveLabelsAsync(
        Guid ownerUserId,
        IReadOnlyCollection<Guid> captureIds,
        IReadOnlyCollection<LabelAssignmentDto> labels)
    {
        var normalizedLabels = NormalizeLabels(labels);
        var captures = await _unitOfWork.RawCaptures.GetByIdsWithGraphAsync(ownerUserId, captureIds);
        if (captures.Count == 0 || normalizedLabels.Count == 0)
        {
            return CreateMutationResult(captureIds.Count, captures.Count, 0);
        }

        var normalizedLookup = new HashSet<string>(
            normalizedLabels.Select(label => $"{label.Category.ToLowerInvariant()}::{label.Value.ToLowerInvariant()}"));

        var mutatedCount = 0;
        foreach (var capture in captures)
        {
            var removedFromCapture = capture.LabelAssignments.RemoveAll(assignment =>
            {
                var key = $"{assignment.LabelCategory.Name.ToLowerInvariant()}::{assignment.LabelValue.Value.ToLowerInvariant()}";
                return normalizedLookup.Contains(key);
            }) > 0;

            var removedFromInsight = capture.ProcessedInsight != null &&
                                     capture.ProcessedInsight.LabelAssignments.RemoveAll(assignment =>
                                     {
                                         var key = $"{assignment.LabelCategory.Name.ToLowerInvariant()}::{assignment.LabelValue.Value.ToLowerInvariant()}";
                                         return normalizedLookup.Contains(key);
                                     }) > 0;

            if (!removedFromCapture && !removedFromInsight)
            {
                continue;
            }

            mutatedCount++;
            await _unitOfWork.RawCaptures.UpdateAsync(capture);
            if (removedFromInsight && capture.ProcessedInsight != null)
            {
                await _unitOfWork.ProcessedInsights.UpdateAsync(capture.ProcessedInsight);
            }
        }

        if (mutatedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return CreateMutationResult(captureIds.Count, captures.Count, mutatedCount);
    }

    public async Task<int> DeleteCapturesAsync(Guid ownerUserId, IReadOnlyCollection<Guid> captureIds)
    {
        var deletedCount = await _unitOfWork.RawCaptures.DeleteByIdsAsync(ownerUserId, captureIds);
        _logger.LogInformation(
            "Deleted {DeletedCount} captures for owner {OwnerUserId} by bulk assistant action",
            deletedCount,
            ownerUserId);
        return deletedCount;
    }

    private static CaptureBulkMutationResult CreateMutationResult(int requestedCount, int matchedCount, int mutatedCount)
    {
        return new CaptureBulkMutationResult
        {
            RequestedCount = requestedCount,
            MatchedCount = matchedCount,
            MutatedCount = mutatedCount
        };
    }

    private static CaptureBulkPreviewItem MapSearchPreviewItem(CaptureSearchRecord record)
    {
        _ = TryExtractSkipMetadata(record.Metadata, out var skipCode, out var skipReason);

        return new CaptureBulkPreviewItem
        {
            CaptureId = record.CaptureId,
            SourceUrl = record.SourceUrl,
            ContentType = record.ContentType.ToString(),
            Status = record.Status.ToString(),
            Similarity = record.Similarity,
            MatchReason = ResolveMatchReason(record),
            PreviewText = BuildPreviewText(record.RawContent),
            SkipCode = skipCode,
            SkipReason = skipReason,
            CreatedAt = record.CreatedAt
        };
    }

    private static string? ResolveMatchReason(CaptureSearchRecord record)
    {
        if (record.MatchedBySemantic && record.MatchedByText)
        {
            return "semantic+text";
        }

        if (record.MatchedBySemantic)
        {
            return "semantic";
        }

        if (record.MatchedByText)
        {
            return "text";
        }

        return null;
    }

    private static string BuildSearchSummary(
        int totalCount,
        int pageCount,
        int page,
        int pageSize,
        int snapshotCount,
        string? query)
    {
        var totalPart = totalCount == 1 ? "Found 1 capture." : $"Found {totalCount} captures.";
        var pagePart = pageCount == 1
            ? $"Showing 1 result on page {page} (page size {pageSize})."
            : $"Showing {pageCount} results on page {page} (page size {pageSize}).";

        var queryPart = string.IsNullOrWhiteSpace(query)
            ? string.Empty
            : $" Query: \"{query}\".";

        if (totalCount > snapshotCount)
        {
            return $"{totalPart} {pagePart} Stored the first {snapshotCount} IDs for safe follow-up actions.{queryPart}";
        }

        return $"{totalPart} {pagePart}{queryPart}";
    }

    private static string BuildPreviewText(string rawContent)
    {
        var normalized = rawContent.Trim();
        if (normalized.Length <= 180)
        {
            return normalized;
        }

        return normalized[..177] + "...";
    }

    private static string? NormalizeQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        return query.Trim();
    }

    private static bool HasAtLeastOneSearchCriterion(
        string? query,
        IReadOnlyCollection<string> tags,
        IReadOnlyCollection<LabelRecord> labels,
        ContentType? contentType,
        CaptureStatus? status,
        DateTime? dateFrom,
        DateTime? dateTo)
    {
        return query is not null
               || tags.Count > 0
               || labels.Count > 0
               || contentType.HasValue
               || status.HasValue
               || dateFrom.HasValue
               || dateTo.HasValue;
    }

    private static (string sortField, string sortDirection) NormalizeSort(
        string? requestedSortField,
        string? requestedSortDirection,
        bool hasQuery)
    {
        var normalizedSortField = NormalizeSortField(requestedSortField);
        if (normalizedSortField == null)
        {
            normalizedSortField = hasQuery
                ? CaptureSearchSortFields.Relevance
                : CaptureSearchSortFields.CreatedAt;
        }

        if (!hasQuery && string.Equals(normalizedSortField, CaptureSearchSortFields.Relevance, StringComparison.OrdinalIgnoreCase))
        {
            normalizedSortField = CaptureSearchSortFields.CreatedAt;
            return (normalizedSortField, SearchSortDirections.Desc);
        }

        var normalizedSortDirection = SearchSortDirections.IsValid(requestedSortDirection)
            ? requestedSortDirection!.Trim().ToLowerInvariant()
            : SearchSortDirections.Desc;

        return (normalizedSortField, normalizedSortDirection);
    }

    private static string? NormalizeSortField(string? requestedSortField)
    {
        if (!CaptureSearchSortFields.IsValid(requestedSortField))
        {
            return null;
        }

        var normalized = requestedSortField!.Trim();
        return normalized switch
        {
            _ when string.Equals(normalized, CaptureSearchSortFields.Relevance, StringComparison.OrdinalIgnoreCase) =>
                CaptureSearchSortFields.Relevance,
            _ when string.Equals(normalized, CaptureSearchSortFields.CreatedAt, StringComparison.OrdinalIgnoreCase) =>
                CaptureSearchSortFields.CreatedAt,
            _ when string.Equals(normalized, CaptureSearchSortFields.Status, StringComparison.OrdinalIgnoreCase) =>
                CaptureSearchSortFields.Status,
            _ when string.Equals(normalized, CaptureSearchSortFields.ContentType, StringComparison.OrdinalIgnoreCase) =>
                CaptureSearchSortFields.ContentType,
            _ => CaptureSearchSortFields.SourceUrl
        };
    }

    private static bool UpsertRawCaptureLabel(RawCapture capture, LabelCategory category, LabelValue value)
    {
        var existing = capture.LabelAssignments.FirstOrDefault(assignment => assignment.LabelCategoryId == category.Id);
        if (existing == null)
        {
            capture.LabelAssignments.Add(new RawCaptureLabelAssignment
            {
                RawCaptureId = capture.Id,
                LabelCategoryId = category.Id,
                LabelCategory = category,
                LabelValueId = value.Id,
                LabelValue = value
            });
            return true;
        }

        if (existing.LabelValueId == value.Id)
        {
            return false;
        }

        existing.LabelValueId = value.Id;
        existing.LabelValue = value;
        existing.LabelCategory = category;
        return true;
    }

    private static bool UpsertProcessedInsightLabel(ProcessedInsight insight, LabelCategory category, LabelValue value)
    {
        var existing = insight.LabelAssignments.FirstOrDefault(assignment => assignment.LabelCategoryId == category.Id);
        if (existing == null)
        {
            insight.LabelAssignments.Add(new ProcessedInsightLabelAssignment
            {
                ProcessedInsightId = insight.Id,
                LabelCategoryId = category.Id,
                LabelCategory = category,
                LabelValueId = value.Id,
                LabelValue = value
            });
            return true;
        }

        if (existing.LabelValueId == value.Id)
        {
            return false;
        }

        existing.LabelValueId = value.Id;
        existing.LabelValue = value;
        existing.LabelCategory = category;
        return true;
    }

    private static HashSet<string> NormalizeTags(IReadOnlyCollection<string> tags)
    {
        return new HashSet<string>(
            tags.Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrWhiteSpace(tag)),
            StringComparer.OrdinalIgnoreCase);
    }

    private static List<LabelAssignmentDto> NormalizeLabels(IReadOnlyCollection<LabelAssignmentDto> labels)
    {
        var normalized = new Dictionary<string, LabelAssignmentDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var label in labels)
        {
            var category = label.Category.Trim();
            var value = label.Value.Trim();
            if (category.Length == 0 || value.Length == 0)
            {
                continue;
            }

            normalized[$"{category}::{value}"] = new LabelAssignmentDto
            {
                Category = category,
                Value = value
            };
        }

        return normalized.Values.ToList();
    }

    private static bool TryExtractSkipMetadata(string? metadata, out string skipCode, out string? skipReason)
    {
        skipCode = string.Empty;
        skipReason = null;

        if (string.IsNullOrWhiteSpace(metadata))
        {
            return false;
        }

        try
        {
            using var json = JsonDocument.Parse(metadata);
            if (!json.RootElement.TryGetProperty("processingSkipCode", out var codeElement) ||
                codeElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            skipCode = codeElement.GetString() ?? string.Empty;
            if (json.RootElement.TryGetProperty("processingSkipReason", out var reasonElement) &&
                reasonElement.ValueKind == JsonValueKind.String)
            {
                skipReason = reasonElement.GetString();
            }

            return !string.IsNullOrWhiteSpace(skipCode);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
