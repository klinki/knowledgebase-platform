using Microsoft.Extensions.Logging;
using Sentinel.Application.Dtos;
using Sentinel.Application.Interfaces;
using Sentinel.Application.Mapping;
using Sentinel.Domain.Entities;
using Sentinel.Domain.Enums;

namespace Sentinel.Application.Services;

public sealed class CaptureService : ICaptureService
{
    private readonly IRawCaptureRepository _rawCaptureRepository;
    private readonly IProcessedInsightRepository _processedInsightRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITextCleaner _textCleaner;
    private readonly IInsightExtractionService _insightExtractionService;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<CaptureService> _logger;

    public CaptureService(
        IRawCaptureRepository rawCaptureRepository,
        IProcessedInsightRepository processedInsightRepository,
        ITagRepository tagRepository,
        IUnitOfWork unitOfWork,
        ITextCleaner textCleaner,
        IInsightExtractionService insightExtractionService,
        IEmbeddingService embeddingService,
        ILogger<CaptureService> logger)
    {
        _rawCaptureRepository = rawCaptureRepository;
        _processedInsightRepository = processedInsightRepository;
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
        _textCleaner = textCleaner;
        _insightExtractionService = insightExtractionService;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<CaptureResponse> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken)
    {
        var rawCapture = new RawCapture
        {
            Id = Guid.NewGuid(),
            SourceId = request.SourceId.Trim(),
            Source = request.Source,
            RawText = request.RawText,
            Url = request.Url,
            AuthorHandle = request.AuthorHandle,
            CapturedAt = request.CapturedAt == default ? DateTimeOffset.UtcNow : request.CapturedAt,
            Status = ProcessingStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _rawCaptureRepository.AddAsync(rawCapture, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var cleanText = _textCleaner.Clean(request.RawText);
            var extraction = await _insightExtractionService.ExtractAsync(cleanText, cancellationToken);
            var embedding = await _embeddingService.GenerateAsync($"{extraction.Summary} {extraction.Insight}", cancellationToken);
            var tags = await _tagRepository.GetOrCreateAsync(extraction.Tags, cancellationToken);

            var insight = new ProcessedInsight
            {
                Id = Guid.NewGuid(),
                RawCaptureId = rawCapture.Id,
                RawCapture = rawCapture,
                Summary = extraction.Summary,
                Insight = extraction.Insight,
                Sentiment = extraction.Sentiment,
                CleanText = cleanText,
                Embedding = embedding,
                CreatedAt = DateTimeOffset.UtcNow
            };

            foreach (var tag in tags)
            {
                insight.Tags.Add(new ProcessedInsightTag
                {
                    ProcessedInsightId = insight.Id,
                    ProcessedInsight = insight,
                    TagId = tag.Id,
                    Tag = tag
                });
            }

            await _processedInsightRepository.AddAsync(insight, cancellationToken);

            rawCapture.Status = ProcessingStatus.Processed;
            rawCapture.ProcessedAt = DateTimeOffset.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CaptureResponse
            {
                CaptureId = rawCapture.Id,
                Status = rawCapture.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process capture {CaptureId}", rawCapture.Id);
            rawCapture.Status = ProcessingStatus.Failed;
            rawCapture.ProcessedAt = DateTimeOffset.UtcNow;
            rawCapture.ErrorMessage = ex.Message;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CaptureResponse
            {
                CaptureId = rawCapture.Id,
                Status = rawCapture.Status
            };
        }
    }

    public async Task<CaptureDetailsResponse?> GetCaptureAsync(Guid captureId, CancellationToken cancellationToken)
    {
        var capture = await _rawCaptureRepository.GetByIdAsync(captureId, cancellationToken);

        if (capture is null)
        {
            return null;
        }

        return new CaptureDetailsResponse
        {
            CaptureId = capture.Id,
            Status = capture.Status,
            ErrorMessage = capture.ErrorMessage,
            ProcessedAt = capture.ProcessedAt,
            Insight = capture.ProcessedInsight is null ? null : ProcessedInsightMapper.ToDto(capture.ProcessedInsight)
        };
    }
}
