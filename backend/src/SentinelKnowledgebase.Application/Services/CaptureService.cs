using System.Text.Json;
using Microsoft.Extensions.Logging;
using Pgvector;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Application.Services.Background;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Application.Services;

public class CaptureService : ICaptureService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContentProcessor _contentProcessor;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly ILogger<CaptureService> _logger;

    public CaptureService(
        IUnitOfWork unitOfWork,
        IContentProcessor contentProcessor,
        IBackgroundTaskQueue backgroundTaskQueue,
        ILogger<CaptureService> logger)
    {
        _unitOfWork = unitOfWork;
        _contentProcessor = contentProcessor;
        _backgroundTaskQueue = backgroundTaskQueue;
        _logger = logger;
    }

    public async Task<CaptureResponseDto> CreateCaptureAsync(CaptureRequestDto request)
    {
        var rawCapture = new RawCapture
        {
            Id = Guid.NewGuid(),
            SourceUrl = request.SourceUrl,
            ContentType = request.ContentType,
            RawContent = request.RawContent,
            Metadata = request.Metadata,
            Status = CaptureStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        if (request.Tags != null && request.Tags.Any())
        {
            foreach (var tagName in request.Tags)
            {
                var tag = await _unitOfWork.Tags.GetByNameAsync(tagName);
                if (tag == null)
                {
                    tag = new Tag { Id = Guid.NewGuid(), Name = tagName };
                    await _unitOfWork.Tags.AddAsync(tag);
                }
                rawCapture.Tags.Add(tag);
            }
        }

        await _unitOfWork.RawCaptures.AddAsync(rawCapture);
        await _unitOfWork.SaveChangesAsync();

        // Queue background processing instead of fire-and-forget Task.Run
        var captureId = rawCapture.Id;
        await _backgroundTaskQueue.QueueAsync(async (serviceProvider, cancellationToken) =>
        {
            var unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
            var contentProcessor = serviceProvider.GetRequiredService<IContentProcessor>();
            var logger = serviceProvider.GetRequiredService<ILogger<CaptureService>>();

            await ProcessCaptureAsync(captureId, unitOfWork, contentProcessor, logger, cancellationToken);
        });

        return MapToResponse(rawCapture);
    }

    public async Task<CaptureResponseDto?> GetCaptureByIdAsync(Guid id)
    {
        var rawCapture = await _unitOfWork.RawCaptures.GetByIdAsync(id);
        return rawCapture != null ? MapToResponse(rawCapture) : null;
    }

    public async Task<IEnumerable<CaptureResponseDto>> GetAllCapturesAsync()
    {
        var rawCaptures = await _unitOfWork.RawCaptures.GetAllAsync();
        return rawCaptures.Select(MapToResponse);
    }

    public async Task<PagedResult<CaptureResponseDto>> GetCapturesPagedAsync(int page, int pageSize)
    {
        // Validate parameters
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var allCaptures = await _unitOfWork.RawCaptures.GetAllAsync();

        var totalCount = allCaptures.Count();
        var items = allCaptures
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToResponse)
            .ToList();

        return new PagedResult<CaptureResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task DeleteCaptureAsync(Guid id)
    {
        await _unitOfWork.RawCaptures.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Process a capture in the background with proper error handling and logging
    /// </summary>
    private static async Task ProcessCaptureAsync(
        Guid rawCaptureId,
        IUnitOfWork unitOfWork,
        IContentProcessor contentProcessor,
        ILogger<CaptureService> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var rawCapture = await unitOfWork.RawCaptures.GetByIdAsync(rawCaptureId);
            if (rawCapture == null)
            {
                logger.LogWarning("Capture {CaptureId} not found for processing", rawCaptureId);
                return;
            }

            logger.LogInformation("Processing capture {CaptureId}", rawCaptureId);

            rawCapture.Status = CaptureStatus.Processing;
            await unitOfWork.RawCaptures.UpdateAsync(rawCapture);
            await unitOfWork.SaveChangesAsync();

            var deNoisedContent = contentProcessor.DenoiseContent(rawCapture.RawContent);
            var insights = await contentProcessor.ExtractInsightsAsync(deNoisedContent, rawCapture.ContentType);
            var embedding = await contentProcessor.GenerateEmbeddingAsync(insights.Summary);

            var processedInsight = new ProcessedInsight
            {
                Id = Guid.NewGuid(),
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

            await unitOfWork.ProcessedInsights.AddAsync(processedInsight);

            var embeddingVector = new EmbeddingVector
            {
                Id = Guid.NewGuid(),
                ProcessedInsightId = processedInsight.Id,
                Vector = new Vector(embedding)
            };

            await unitOfWork.EmbeddingVectors.AddAsync(embeddingVector);

            rawCapture.Status = CaptureStatus.Completed;
            rawCapture.ProcessedAt = DateTime.UtcNow;
            await unitOfWork.RawCaptures.UpdateAsync(rawCapture);

            await unitOfWork.SaveChangesAsync();

            logger.LogInformation("Successfully processed capture {CaptureId}", rawCaptureId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process capture {CaptureId}", rawCaptureId);

            try
            {
                // Try to mark the capture as failed
                var rawCapture = await unitOfWork.RawCaptures.GetByIdAsync(rawCaptureId);
                if (rawCapture != null)
                {
                    rawCapture.Status = CaptureStatus.Failed;
                    rawCapture.ProcessedAt = DateTime.UtcNow;
                    await unitOfWork.RawCaptures.UpdateAsync(rawCapture);
                    await unitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception updateEx)
            {
                logger.LogError(updateEx, "Failed to update capture {CaptureId} status to Failed", rawCaptureId);
            }
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
            Tags = rawCapture.Tags.Select(t => t.Name).ToList(),
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
                Tags = rawCapture.ProcessedInsight.Tags.Select(t => t.Name).ToList()
            } : null
        };
    }
}
