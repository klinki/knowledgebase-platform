using System.Diagnostics;
using System.Text.Json;
using Hangfire;
using SentinelKnowledgebase.Application.DTOs.Labels;
using Microsoft.Extensions.Logging;
using Pgvector;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Application.Localization;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Application.Services;

public class CaptureService : ICaptureService
{
    private const string ProcessingErrorMetadataKey = "lastProcessingError";
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContentProcessor _contentProcessor;
    private readonly IUserLanguagePreferencesService _userLanguagePreferencesService;
    private readonly IMonitoringService _monitoringService;
    private readonly ICaptureProcessingAdminService _captureProcessingAdminService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<CaptureService> _logger;
    
    public CaptureService(
        IUnitOfWork unitOfWork,
        IContentProcessor contentProcessor,
        IUserLanguagePreferencesService userLanguagePreferencesService,
        IMonitoringService monitoringService,
        ICaptureProcessingAdminService captureProcessingAdminService,
        IBackgroundJobClient backgroundJobClient,
        ILogger<CaptureService> logger)
    {
        _unitOfWork = unitOfWork;
        _contentProcessor = contentProcessor;
        _userLanguagePreferencesService = userLanguagePreferencesService;
        _monitoringService = monitoringService;
        _captureProcessingAdminService = captureProcessingAdminService;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }
    
    public async Task<CaptureResponseDto> CreateCaptureAsync(Guid ownerUserId, CaptureRequestDto request)
    {
        var responses = await CreateCapturesAsync(ownerUserId, [request]);
        return responses[0];
    }

    public async Task<IReadOnlyList<CaptureResponseDto>> CreateCapturesAsync(Guid ownerUserId, IReadOnlyList<CaptureRequestDto> requests)
    {
        if (requests.Count == 0)
        {
            return [];
        }

        var existingTags = (await _unitOfWork.Tags.GetAllAsync(ownerUserId))
            .ToDictionary(tag => tag.Name, StringComparer.OrdinalIgnoreCase);

        var existingCategories = (await _unitOfWork.LabelCategories.GetAllWithValuesAsync(ownerUserId)).ToList();
        var categoriesByName = existingCategories
            .ToDictionary(category => category.Name, StringComparer.OrdinalIgnoreCase);
        var valuesByCategoryId = existingCategories.ToDictionary(
            category => category.Id,
            category => category.Values.ToDictionary(value => value.Value, StringComparer.OrdinalIgnoreCase));

        var responses = new List<CaptureResponseDto>(requests.Count);

        foreach (var request in requests)
        {
            var rawCapture = new RawCapture
            {
                Id = Guid.NewGuid(),
                OwnerUserId = ownerUserId,
                SourceUrl = request.SourceUrl,
                ContentType = request.ContentType,
                RawContent = request.RawContent,
                Metadata = request.Metadata,
                Status = CaptureStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await AttachTagsAsync(ownerUserId, rawCapture, request, existingTags);

            var resolvedLabels = await ResolveRawCaptureLabelsAsync(
                ownerUserId,
                rawCapture.Id,
                request,
                categoriesByName,
                valuesByCategoryId);
            rawCapture.LabelAssignments.AddRange(resolvedLabels);

            await _unitOfWork.RawCaptures.AddAsync(rawCapture);
            responses.Add(MapToResponse(rawCapture));
        }

        await _unitOfWork.SaveChangesAsync();
        return responses;
    }
    
    public async Task<CaptureResponseDto?> GetCaptureByIdAsync(Guid ownerUserId, Guid id)
    {
        var rawCapture = await _unitOfWork.RawCaptures.GetByIdAsync(id, ownerUserId);
        return rawCapture != null ? MapToResponse(rawCapture) : null;
    }

    public async Task<CaptureListPageDto> GetCaptureListPageAsync(Guid ownerUserId, CaptureListQueryDto query)
    {
        var options = NormalizeListQuery(query);
        var result = await _unitOfWork.RawCaptures.GetPagedListAsync(ownerUserId, options);

        return new CaptureListPageDto
        {
            Items = result.Items.Select(MapToListItem).ToList(),
            TotalCount = result.TotalCount,
            Page = options.Page,
            PageSize = options.PageSize
        };
    }
    
    public async Task<IEnumerable<CaptureResponseDto>> GetAllCapturesAsync(Guid ownerUserId)
    {
        var rawCaptures = await _unitOfWork.RawCaptures.GetAllAsync(ownerUserId);
        return rawCaptures.Select(MapToResponse);
    }
    
    public async Task<bool> DeleteCaptureAsync(Guid ownerUserId, Guid id)
    {
        var existingCapture = await _unitOfWork.RawCaptures.GetByIdAsync(id, ownerUserId);
        if (existingCapture == null)
        {
            return false;
        }

        await _unitOfWork.RawCaptures.DeleteAsync(id, ownerUserId);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
    

    public async Task<bool> RetryCaptureAsync(Guid ownerUserId, Guid id)
    {
        var capture = await _unitOfWork.RawCaptures.GetByIdAsync(id, ownerUserId);
        if (capture == null)
        {
            return false;
        }

        if (capture.Status == CaptureStatus.Completed)
        {
            return false;
        }

        capture.Status = CaptureStatus.Pending;
        capture.ProcessedAt = null;
        capture.Metadata = ClearProcessingError(capture.Metadata);

        await _unitOfWork.RawCaptures.UpdateAsync(capture);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task ProcessCaptureAsync(Guid rawCaptureId)
    {
        var stopwatch = Stopwatch.StartNew();
        var processingStatus = "failed";

        try
        {
            _logger.LogInformation("Starting capture processing for {RawCaptureId}", rawCaptureId);

            var rawCapture = await _unitOfWork.RawCaptures.GetByIdAsync(rawCaptureId);
            if (rawCapture == null)
            {
                processingStatus = "not_found";
                _logger.LogWarning("Capture {RawCaptureId} was not found for processing", rawCaptureId);
                return;
            }

            if (rawCapture.Status == CaptureStatus.Completed || rawCapture.Status == CaptureStatus.Processing)
            {
                processingStatus = "skipped";
                _logger.LogInformation(
                    "Capture {RawCaptureId} skipped because it is already {CaptureStatus}",
                    rawCaptureId,
                    rawCapture.Status);
                return;
            }

            if (await _captureProcessingAdminService.IsPausedAsync())
            {
                processingStatus = "paused";
                var jobId = _backgroundJobClient.Schedule<ICaptureService>(
                    service => service.ProcessCaptureAsync(rawCaptureId),
                    TimeSpan.FromSeconds(60));
                _logger.LogInformation(
                    "Capture {RawCaptureId} deferred because processing is paused; retry job {JobId} scheduled",
                    rawCaptureId,
                    jobId);
                return;
            }
            
            rawCapture.Status = CaptureStatus.Processing;
            await _unitOfWork.RawCaptures.UpdateAsync(rawCapture);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation(
                "Capture {RawCaptureId} marked as processing for owner {OwnerUserId}",
                rawCaptureId,
                rawCapture.OwnerUserId);
            
            var deNoisedContent = _contentProcessor.DenoiseContent(rawCapture.RawContent);
            _logger.LogInformation(
                "Capture {RawCaptureId} denoised from {OriginalLength} to {DenoisedLength} characters",
                rawCaptureId,
                rawCapture.RawContent.Length,
                deNoisedContent.Length);

            var languagePreferences = await _userLanguagePreferencesService.GetAsync(rawCapture.OwnerUserId);
            var sourceLanguageCode = GetSourceLanguageCode(rawCapture.Metadata);
            var outputLanguageCode = ResolveInsightOutputLanguageCode(sourceLanguageCode, languagePreferences);

            _logger.LogInformation(
                "Capture {RawCaptureId} will generate insights in {OutputLanguageCode} from source language {SourceLanguageCode}",
                rawCaptureId,
                outputLanguageCode ?? "source",
                sourceLanguageCode ?? "unknown");

            var insights = await _contentProcessor.ExtractInsightsAsync(
                deNoisedContent,
                rawCapture.ContentType,
                outputLanguageCode);
            _logger.LogInformation(
                "Capture {RawCaptureId} produced insights with title '{Title}'",
                rawCaptureId,
                insights.Title);

            var embedding = await _contentProcessor.GenerateEmbeddingAsync(insights.Summary);
            _logger.LogInformation(
                "Capture {RawCaptureId} generated embedding with {Dimensions} dimensions",
                rawCaptureId,
                embedding.Length);
            
            var processedInsight = new ProcessedInsight
            {
                Id = Guid.NewGuid(),
                OwnerUserId = rawCapture.OwnerUserId,
                RawCaptureId = rawCaptureId,
                Title = insights.Title,
                Summary = insights.Summary,
                KeyInsights = JsonSerializer.Serialize(insights.KeyInsights),
                ActionItems = JsonSerializer.Serialize(insights.ActionItems),
                SourceTitle = insights.SourceTitle,
                Author = insights.Author,
                ProcessedAt = DateTime.UtcNow
            };
            
            if (rawCapture.Tags.Any())
            {
                processedInsight.Tags = rawCapture.Tags.ToList();
            }

            if (rawCapture.LabelAssignments.Any())
            {
                processedInsight.LabelAssignments = rawCapture.LabelAssignments
                    .Select(assignment => new ProcessedInsightLabelAssignment
                    {
                        ProcessedInsightId = processedInsight.Id,
                        LabelCategoryId = assignment.LabelCategoryId,
                        LabelValueId = assignment.LabelValueId,
                        LabelCategory = assignment.LabelCategory,
                        LabelValue = assignment.LabelValue
                    })
                    .ToList();
            }
            
            await _unitOfWork.ProcessedInsights.AddAsync(processedInsight);
            
            var embeddingVector = new EmbeddingVector
            {
                Id = Guid.NewGuid(),
                ProcessedInsightId = processedInsight.Id,
                Vector = new Vector(embedding)
            };
            
            await _unitOfWork.EmbeddingVectors.AddAsync(embeddingVector);
            
            rawCapture.Status = CaptureStatus.Completed;
            rawCapture.ProcessedAt = DateTime.UtcNow;
            await _unitOfWork.RawCaptures.UpdateAsync(rawCapture);
            
            await _unitOfWork.SaveChangesAsync();

            processingStatus = "completed";
            _monitoringService.IncrementProcessedCaptures();
            _logger.LogInformation(
                "Capture {RawCaptureId} completed successfully as insight {ProcessedInsightId}",
                rawCaptureId,
                processedInsight.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Capture {RawCaptureId} failed during processing", rawCaptureId);

            var failedCapture = await _unitOfWork.RawCaptures.GetByIdAsync(rawCaptureId);
            if (failedCapture != null)
            {
                failedCapture.Metadata = SetProcessingError(
                    failedCapture.Metadata,
                    ex.Message);
                await _unitOfWork.RawCaptures.UpdateAsync(failedCapture);
                await _unitOfWork.SaveChangesAsync();
            }

            throw;
        }
        finally
        {
            stopwatch.Stop();
            _monitoringService.RecordCaptureProcessingDuration(stopwatch.Elapsed.TotalMilliseconds, processingStatus);
            _logger.LogInformation(
                "Capture {RawCaptureId} finished with status {ProcessingStatus} in {ElapsedMilliseconds} ms",
                rawCaptureId,
                processingStatus,
                stopwatch.Elapsed.TotalMilliseconds);
        }
    }
    
    private CaptureResponseDto MapToResponse(RawCapture rawCapture)
    {
        return new CaptureResponseDto
        {
            Id = rawCapture.Id,
            SourceUrl = rawCapture.SourceUrl,
            ContentType = rawCapture.ContentType,
            Status = rawCapture.Status,
            CreatedAt = rawCapture.CreatedAt,
            ProcessedAt = rawCapture.ProcessedAt,
            RawContent = rawCapture.RawContent,
            Metadata = rawCapture.Metadata,
            FailureReason = GetProcessingError(rawCapture.Metadata),
            Tags = rawCapture.Tags.Select(t => t.Name).ToList(),
            Labels = MapLabels(rawCapture.LabelAssignments),
            ProcessedInsight = rawCapture.ProcessedInsight != null ? new ProcessedInsightDto
            {
                Id = rawCapture.ProcessedInsight.Id,
                Title = rawCapture.ProcessedInsight.Title,
                Summary = rawCapture.ProcessedInsight.Summary,
                KeyInsights = rawCapture.ProcessedInsight.KeyInsights,
                ActionItems = rawCapture.ProcessedInsight.ActionItems,
                SourceTitle = rawCapture.ProcessedInsight.SourceTitle,
                Author = rawCapture.ProcessedInsight.Author,
                ProcessedAt = rawCapture.ProcessedInsight.ProcessedAt,
                Tags = rawCapture.ProcessedInsight.Tags.Select(t => t.Name).ToList(),
                Labels = MapLabels(rawCapture.ProcessedInsight.LabelAssignments)
            } : null
        };
    }

    private static CaptureListItemDto MapToListItem(CaptureListRecord capture)
    {
        return new CaptureListItemDto
        {
            Id = capture.Id,
            SourceUrl = capture.SourceUrl,
            ContentType = capture.ContentType,
            Status = capture.Status,
            CreatedAt = capture.CreatedAt,
            ProcessedAt = capture.ProcessedAt,
            FailureReason = GetProcessingError(capture.Metadata)
        };
    }

    private static CaptureListQueryOptions NormalizeListQuery(CaptureListQueryDto query)
    {
        var options = new CaptureListQueryOptions
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 200),
            SortField = NormalizeSortField(query.SortField),
            SortDirection = NormalizeSortDirection(query.SortDirection)
        };

        if (!string.IsNullOrWhiteSpace(query.ContentType))
        {
            options.ContentType = ParseEnumFilter<ContentType>(query.ContentType, "contentType");
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            options.Status = ParseEnumFilter<CaptureStatus>(query.Status, "status");
        }

        return options;
    }

    private static string NormalizeSortField(string? sortField)
    {
        return sortField?.Trim() switch
        {
            "contentType" => "contentType",
            "status" => "status",
            "sourceUrl" => "sourceUrl",
            _ => "createdAt"
        };
    }

    private static string NormalizeSortDirection(string? sortDirection)
    {
        return string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
    }

    private static TEnum ParseEnumFilter<TEnum>(string rawValue, string parameterName)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(rawValue.Trim(), ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new ArgumentException($"Invalid {parameterName} filter.", parameterName);
    }

    private async Task AttachTagsAsync(
        Guid ownerUserId,
        RawCapture rawCapture,
        CaptureRequestDto request,
        IDictionary<string, Tag> tagsByName)
    {
        if (request.Tags == null || request.Tags.Count == 0)
        {
            return;
        }

        var requestTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tagName in request.Tags)
        {
            var normalizedTagName = tagName.Trim();
            if (string.IsNullOrWhiteSpace(normalizedTagName) || !requestTags.Add(normalizedTagName))
            {
                continue;
            }

            if (!tagsByName.TryGetValue(normalizedTagName, out var tag))
            {
                tag = new Tag
                {
                    Id = Guid.NewGuid(),
                    OwnerUserId = ownerUserId,
                    Name = normalizedTagName
                };
                await _unitOfWork.Tags.AddAsync(tag);
                tagsByName[normalizedTagName] = tag;
            }

            rawCapture.Tags.Add(tag);
        }
    }

    private async Task<List<RawCaptureLabelAssignment>> ResolveRawCaptureLabelsAsync(
        Guid ownerUserId,
        Guid rawCaptureId,
        CaptureRequestDto request,
        IDictionary<string, LabelCategory> categoriesByName,
        IDictionary<Guid, Dictionary<string, LabelValue>> valuesByCategoryId)
    {
        var mergedLabels = BuildMergedLabels(request);
        var assignments = new List<RawCaptureLabelAssignment>(mergedLabels.Count);

        foreach (var label in mergedLabels)
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
                categoriesByName[category.Name] = category;
                valuesByCategoryId[category.Id] = new Dictionary<string, LabelValue>(StringComparer.OrdinalIgnoreCase);
            }

            if (!valuesByCategoryId.TryGetValue(category.Id, out var valuesByName))
            {
                valuesByName = new Dictionary<string, LabelValue>(StringComparer.OrdinalIgnoreCase);
                valuesByCategoryId[category.Id] = valuesByName;
            }

            if (!valuesByName.TryGetValue(label.Value, out var value))
            {
                value = new LabelValue
                {
                    Id = Guid.NewGuid(),
                    LabelCategoryId = category.Id,
                    LabelCategory = category,
                    Value = label.Value,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.LabelValues.AddAsync(value);
                valuesByName[value.Value] = value;
            }

            assignments.Add(new RawCaptureLabelAssignment
            {
                RawCaptureId = rawCaptureId,
                LabelCategoryId = category.Id,
                LabelCategory = category,
                LabelValueId = value.Id,
                LabelValue = value
            });
        }

        return assignments;
    }

    private static List<LabelAssignmentDto> BuildMergedLabels(CaptureRequestDto request)
    {
        var labels = new Dictionary<string, LabelAssignmentDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var label in GetAutoLabels(request))
        {
            labels[label.Category] = label;
        }

        if (request.Labels != null)
        {
            foreach (var label in request.Labels)
            {
                var category = label.Category.Trim();
                var value = label.Value.Trim();
                if (category.Length == 0 || value.Length == 0)
                {
                    continue;
                }

                labels[category] = new LabelAssignmentDto
                {
                    Category = category,
                    Value = value
                };
            }
        }

        return labels.Values
            .OrderBy(label => label.Category)
            .ThenBy(label => label.Value)
            .ToList();
    }

    private static IEnumerable<LabelAssignmentDto> GetAutoLabels(CaptureRequestDto request)
    {
        var source = GetMetadataString(request.Metadata, "source")?.Trim().ToLowerInvariant();
        if (source == "twitter" || request.ContentType == ContentType.Tweet)
        {
            yield return new LabelAssignmentDto
            {
                Category = "Source",
                Value = "Twitter"
            };
        }
        else if (source is "webpage" or "webpage_selection")
        {
            yield return new LabelAssignmentDto
            {
                Category = "Source",
                Value = "Web"
            };
        }

        if (source == "webpage")
        {
            var normalizedLanguage = GetSourceLanguageDisplayName(request.Metadata);
            if (!string.IsNullOrWhiteSpace(normalizedLanguage))
            {
                yield return new LabelAssignmentDto
                {
                    Category = "Language",
                    Value = normalizedLanguage
                };
            }
        }
    }

    private static string? GetSourceLanguageCode(string? metadata)
    {
        var language = GetMetadataString(metadata, "metadata", "language")
            ?? GetMetadataString(metadata, "language");

        return LanguageCatalog.NormalizeToBaseLanguageCode(language);
    }

    private static string? GetSourceLanguageDisplayName(string? metadata)
    {
        var sourceLanguageCode = GetSourceLanguageCode(metadata);
        if (!string.IsNullOrWhiteSpace(sourceLanguageCode))
        {
            return LanguageCatalog.GetDisplayName(sourceLanguageCode);
        }

        var rawLanguage = GetMetadataString(metadata, "metadata", "language")
            ?? GetMetadataString(metadata, "language");
        return string.IsNullOrWhiteSpace(rawLanguage)
            ? null
            : rawLanguage.Trim();
    }

    private static string? ResolveInsightOutputLanguageCode(
        string? sourceLanguageCode,
        UserLanguagePreferencesSnapshot languagePreferences)
    {
        if (string.IsNullOrWhiteSpace(sourceLanguageCode))
        {
            return null;
        }

        if (string.Equals(sourceLanguageCode, languagePreferences.DefaultLanguageCode, StringComparison.OrdinalIgnoreCase))
        {
            return sourceLanguageCode;
        }

        if (languagePreferences.PreservedLanguageCodes.Contains(sourceLanguageCode, StringComparer.OrdinalIgnoreCase))
        {
            return sourceLanguageCode;
        }

        return languagePreferences.DefaultLanguageCode;
    }

    private static string? GetMetadataString(string? metadata, params string[] path)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(metadata);
            JsonElement current = document.RootElement;

            foreach (var segment in path)
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
                {
                    return null;
                }
            }

            return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static List<LabelAssignmentDto> MapLabels(IEnumerable<RawCaptureLabelAssignment> assignments)
    {
        return assignments
            .OrderBy(assignment => assignment.LabelCategory.Name)
            .ThenBy(assignment => assignment.LabelValue.Value)
            .Select(assignment => new LabelAssignmentDto
            {
                Category = assignment.LabelCategory.Name,
                Value = assignment.LabelValue.Value
            })
            .ToList();
    }

    private static List<LabelAssignmentDto> MapLabels(IEnumerable<ProcessedInsightLabelAssignment> assignments)
    {
        return assignments
            .OrderBy(assignment => assignment.LabelCategory.Name)
            .ThenBy(assignment => assignment.LabelValue.Value)
            .Select(assignment => new LabelAssignmentDto
            {
                Category = assignment.LabelCategory.Name,
                Value = assignment.LabelValue.Value
            })
            .ToList();
    }

    private static string? GetProcessingError(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return null;
        }

        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(metadata);
            if (values != null
                && values.TryGetValue(ProcessingErrorMetadataKey, out var errorValue)
                && errorValue.ValueKind == JsonValueKind.String)
            {
                return errorValue.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static string SetProcessingError(string? metadata, string message)
    {
        var payload = ParseMetadata(metadata);
        payload[ProcessingErrorMetadataKey] = message;
        return JsonSerializer.Serialize(payload);
    }

    private static string? ClearProcessingError(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return metadata;
        }

        var payload = ParseMetadata(metadata);
        if (!payload.Remove(ProcessingErrorMetadataKey))
        {
            return metadata;
        }

        return payload.Count == 0 ? null : JsonSerializer.Serialize(payload);
    }

    private static Dictionary<string, object?> ParseMetadata(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return new Dictionary<string, object?>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(metadata)
                ?? new Dictionary<string, object?>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, object?>();
        }
    }

}
