using Microsoft.Extensions.Diagnostics.HealthChecks;
using SentinelKnowledgebase.Api.BackgroundProcessing;

namespace SentinelKnowledgebase.Api.HealthChecks;

public class CaptureProcessingQueueHealthCheck : IHealthCheck
{
    private readonly ICaptureProcessingQueue _captureProcessingQueue;
    private readonly int _maxQueueLength;

    public CaptureProcessingQueueHealthCheck(
        ICaptureProcessingQueue captureProcessingQueue,
        IConfiguration configuration)
    {
        _captureProcessingQueue = captureProcessingQueue;
        _maxQueueLength = configuration.GetValue<int?>("HealthChecks:CaptureProcessingQueue:MaxQueueLength") ?? 100;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var queueLength = _captureProcessingQueue.QueueLength;

        if (queueLength > _maxQueueLength)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Capture processing queue length {queueLength} exceeded limit {_maxQueueLength}."));
        }

        return Task.FromResult(HealthCheckResult.Healthy($"Capture processing queue length is {queueLength}."));
    }
}
