using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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
    
    public CaptureService(IUnitOfWork unitOfWork, IContentProcessor contentProcessor)
    {
        _unitOfWork = unitOfWork;
        _contentProcessor = contentProcessor;
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
        
        _ = Task.Run(async () => await ProcessCaptureAsync(rawCapture.Id));
        
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
    
    public async Task DeleteCaptureAsync(Guid id)
    {
        await _unitOfWork.RawCaptures.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }
    
    private async Task ProcessCaptureAsync(Guid rawCaptureId)
    {
        try
        {
            var rawCapture = await _unitOfWork.RawCaptures.GetByIdAsync(rawCaptureId);
            if (rawCapture == null) return;
            
            rawCapture.Status = CaptureStatus.Processing;
            await _unitOfWork.RawCaptures.UpdateAsync(rawCapture);
            await _unitOfWork.SaveChangesAsync();
            
            var deNoisedContent = _contentProcessor.DenoiseContent(rawCapture.RawContent);
            var insights = await _contentProcessor.ExtractInsightsAsync(deNoisedContent, rawCapture.ContentType);
            var embedding = await _contentProcessor.GenerateEmbeddingAsync(insights.Summary);
            
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
            
            await _unitOfWork.ProcessedInsights.AddAsync(processedInsight);
            
            var embeddingVector = new EmbeddingVector
            {
                Id = Guid.NewGuid(),
                ProcessedInsightId = processedInsight.Id,
                Vector = embedding
            };
            
            await _unitOfWork.EmbeddingVectors.AddAsync(embeddingVector);
            
            rawCapture.Status = CaptureStatus.Completed;
            rawCapture.ProcessedAt = DateTime.UtcNow;
            await _unitOfWork.RawCaptures.UpdateAsync(rawCapture);
            
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception)
        {
            var rawCapture = await _unitOfWork.RawCaptures.GetByIdAsync(rawCaptureId);
            if (rawCapture != null)
            {
                rawCapture.Status = CaptureStatus.Failed;
                await _unitOfWork.RawCaptures.UpdateAsync(rawCapture);
                await _unitOfWork.SaveChangesAsync();
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
