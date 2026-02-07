using SentinelKnowledgebase.Application.Services.Background;

namespace SentinelKnowledgebase.Api.Services;

/// <summary>
/// Hosted service that processes background tasks from the queue
/// </summary>
public class CaptureProcessingHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<CaptureProcessingHostedService> _logger;

    public CaptureProcessingHostedService(
        IServiceProvider serviceProvider,
        IBackgroundTaskQueue taskQueue,
        ILogger<CaptureProcessingHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _taskQueue = taskQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background processing service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                try
                {
                    // Create a scope for each work item to ensure proper DbContext lifecycle
                    using var scope = _serviceProvider.CreateScope();
                    await workItem(scope.ServiceProvider, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing background task");
                    // Don't rethrow - continue processing other tasks
                }
            }
            catch (OperationCanceledException)
            {
                // Shutdown requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background processing loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Background processing service stopped");
    }
}
