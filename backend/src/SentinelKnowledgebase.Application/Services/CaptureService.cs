using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Pgvector;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Application.Services;

public class CaptureService : ICaptureService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContentProcessor _contentProcessor;
    private readonly IMonitoringService _monitoringService;
    private readonly ILogger<CaptureService> _logger;
    
    public CaptureService(
        IUnitOfWork unitOfWork,
        IContentProcessor contentProcessor,
        IMonitoringService monitoringService,
        ILogger<CaptureService> logger)
    {
        _unitOfWork = unitOfWork;
        _contentProcessor = contentProcessor;
        _monitoringService = monitoringService;
        _logger = logger;
    }
    
    public async Task<CaptureResponseDto> CreateCaptureAsync(Guid ownerUserId, CaptureRequestDto request)
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
        
        if (request.Tags != null && request.Tags.Any())
        {
            foreach (var tagName in request.Tags)
            {
                var normalizedTagName = tagName.Trim();
                if (string.IsNullOrWhiteSpace(normalizedTagName))
                {
                    continue;
                }

                var tag = await _unitOfWork.Tags.GetByNameAsync(ownerUserId, normalizedTagName);
                if (tag == null)
                {
                    tag = new Tag
                    {
                        Id = Guid.NewGuid(),
                        OwnerUserId = ownerUserId,
                        Name = normalizedTagName
                    };
                    await _unitOfWork.Tags.AddAsync(tag);
                }
                rawCapture.Tags.Add(tag);
            }
        }
        
        await _unitOfWork.RawCaptures.AddAsync(rawCapture);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(rawCapture);
    }
    
    public async Task<CaptureResponseDto?> GetCaptureByIdAsync(Guid ownerUserId, Guid id)
    {
        var rawCapture = await _unitOfWork.RawCaptures.GetByIdAsync(id, ownerUserId);
        return rawCapture != null ? MapToResponse(rawCapture) : null;
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

            var insights = await _contentProcessor.ExtractInsightsAsync(deNoisedContent, rawCapture.ContentType);
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
