using SentinelKnowledgebase.Application.DTOs;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Application.Services;

public class CaptureService : ICaptureService
{
    private readonly IRawCaptureRepository _captureRepository;
    private readonly ITagRepository _tagRepository;

    public CaptureService(IRawCaptureRepository captureRepository, ITagRepository tagRepository)
    {
        _captureRepository = captureRepository;
        _tagRepository = tagRepository;
    }

    public async Task<CaptureResponse> CreateCaptureAsync(CaptureRequest request, CancellationToken cancellationToken = default)
    {
        var capture = new RawCapture
        {
            SourceUrl = request.SourceUrl,
            RawContent = request.RawContent,
            Source = request.Source,
            Status = CaptureStatus.Pending
        };

        // Process tags
        foreach (var tagName in request.Tags.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var existingTag = await _tagRepository.GetByNameAsync(tagName, cancellationToken);
            if (existingTag != null)
            {
                capture.Tags.Add(existingTag);
            }
            else
            {
                var newTag = new Tag { Name = tagName };
                await _tagRepository.AddAsync(newTag, cancellationToken);
                capture.Tags.Add(newTag);
            }
        }

        var created = await _captureRepository.AddAsync(capture, cancellationToken);

        return new CaptureResponse
        {
            Id = created.Id,
            SourceUrl = created.SourceUrl,
            Source = created.Source,
            Status = created.Status,
            CreatedAt = created.CreatedAt
        };
    }

    public async Task<InsightResponse?> GetInsightAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var capture = await _captureRepository.GetByIdAsync(id, cancellationToken);
        
        if (capture?.ProcessedInsight == null)
        {
            return null;
        }

        return new InsightResponse
        {
            Id = capture.ProcessedInsight.Id,
            RawCaptureId = capture.ProcessedInsight.RawCaptureId,
            Title = capture.ProcessedInsight.Title,
            Summary = capture.ProcessedInsight.Summary,
            CleanContent = capture.ProcessedInsight.CleanContent,
            ProcessedAt = capture.ProcessedInsight.ProcessedAt,
            Tags = capture.Tags.Select(t => t.Name).ToList()
        };
    }
}
