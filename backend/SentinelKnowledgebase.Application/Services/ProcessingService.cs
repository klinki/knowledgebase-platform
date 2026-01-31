using Pgvector;
using SentinelKnowledgebase.Application.DTOs;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Application.Services;

public class ProcessingService : IProcessingService
{
    private readonly IRawCaptureRepository _captureRepository;
    private readonly IProcessedInsightRepository _insightRepository;

    public ProcessingService(
        IRawCaptureRepository captureRepository,
        IProcessedInsightRepository insightRepository)
    {
        _captureRepository = captureRepository;
        _insightRepository = insightRepository;
    }

    public async Task ProcessPendingCapturesAsync(CancellationToken cancellationToken = default)
    {
        var pendingCaptures = await _captureRepository.GetPendingAsync(cancellationToken);

        foreach (var capture in pendingCaptures)
        {
            await ProcessCaptureAsync(capture.Id, cancellationToken);
        }
    }

    public async Task<InsightResponse?> ProcessCaptureAsync(Guid captureId, CancellationToken cancellationToken = default)
    {
        var capture = await _captureRepository.GetByIdAsync(captureId, cancellationToken);
        
        if (capture == null)
        {
            return null;
        }

        // Update status to Processing
        capture.Status = CaptureStatus.Processing;
        await _captureRepository.UpdateAsync(capture, cancellationToken);

        try
        {
            // Step 1: De-noise content (basic cleaning)
            var cleanContent = CleanContent(capture.RawContent);

            // Step 2: Extract insights (placeholder - would use OpenAI in production)
            var (title, summary) = ExtractInsights(cleanContent);

            // Step 3: Generate embedding (placeholder - would use OpenAI in production)
            var embedding = GenerateEmbedding(cleanContent);

            // Step 4: Store processed insight
            var insight = new ProcessedInsight
            {
                RawCaptureId = capture.Id,
                Title = title,
                Summary = summary,
                CleanContent = cleanContent,
                Embedding = embedding
            };

            await _insightRepository.AddAsync(insight, cancellationToken);

            // Update capture status
            capture.Status = CaptureStatus.Completed;
            capture.ProcessedAt = DateTime.UtcNow;
            await _captureRepository.UpdateAsync(capture, cancellationToken);

            return new InsightResponse
            {
                Id = insight.Id,
                RawCaptureId = insight.RawCaptureId,
                Title = insight.Title,
                Summary = insight.Summary,
                CleanContent = insight.CleanContent,
                ProcessedAt = insight.ProcessedAt,
                Tags = capture.Tags.Select(t => t.Name).ToList()
            };
        }
        catch
        {
            capture.Status = CaptureStatus.Failed;
            await _captureRepository.UpdateAsync(capture, cancellationToken);
            throw;
        }
    }

    private static string CleanContent(string rawContent)
    {
        // Basic content cleaning
        // In production, this could include HTML stripping, normalization, etc.
        return rawContent.Trim();
    }

    private static (string title, string summary) ExtractInsights(string content)
    {
        // Placeholder implementation
        // In production, this would call OpenAI API
        var title = content.Length > 50 
            ? content.Substring(0, 50) + "..." 
            : content;
        
        var summary = content.Length > 200 
            ? content.Substring(0, 200) + "..." 
            : content;

        return (title, summary);
    }

    private static Vector GenerateEmbedding(string content)
    {
        // Placeholder implementation - returns zero vector
        // In production, this would call OpenAI API to generate embeddings
        return new Vector(new float[1536]);
    }
}
