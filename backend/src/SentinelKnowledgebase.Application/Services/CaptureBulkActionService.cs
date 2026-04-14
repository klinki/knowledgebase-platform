using System.Text.Json;
using Microsoft.Extensions.Logging;
using SentinelKnowledgebase.Application.DTOs.Labels;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
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
    private readonly ILogger<CaptureBulkActionService> _logger;

    public CaptureBulkActionService(IUnitOfWork unitOfWork, ILogger<CaptureBulkActionService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
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
