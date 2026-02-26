using SentinelKnowledgebase.Application.Services.Interfaces;

namespace SentinelKnowledgebase.Api.BackgroundProcessing;

public class CaptureProcessingBackgroundService : BackgroundService
{
    private readonly ICaptureProcessingQueue _captureProcessingQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<CaptureProcessingBackgroundService> _logger;

    public CaptureProcessingBackgroundService(
        ICaptureProcessingQueue captureProcessingQueue,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<CaptureProcessingBackgroundService> logger)
    {
        _captureProcessingQueue = captureProcessingQueue;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var rawCaptureId in _captureProcessingQueue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var captureService = scope.ServiceProvider.GetRequiredService<ICaptureService>();
                await captureService.ProcessCaptureAsync(rawCaptureId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed processing capture {CaptureId}", rawCaptureId);
            }
        }
    }
}
