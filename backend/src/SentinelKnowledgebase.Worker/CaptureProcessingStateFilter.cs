using System.Text.Json;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Worker;

public sealed class CaptureProcessingStateFilter : IApplyStateFilter
{
    private const string ProcessingErrorMetadataKey = "lastProcessingError";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CaptureProcessingStateFilter> _logger;

    public CaptureProcessingStateFilter(
        IServiceScopeFactory scopeFactory,
        ILogger<CaptureProcessingStateFilter> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        if (!TryGetRawCaptureId(context.BackgroundJob.Job, out var rawCaptureId))
        {
            return;
        }

        switch (context.NewState)
        {
            case ScheduledState:
                UpdateCaptureStatus(rawCaptureId, CaptureStatus.Pending, null, "pending for retry");
                break;
            case FailedState:
                UpdateCaptureStatus(rawCaptureId, CaptureStatus.Failed, "Processing failed after retries. Please retry from the captures page.", "failed permanently");
                break;
        }
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
    }

    private static bool TryGetRawCaptureId(Job job, out Guid rawCaptureId)
    {
        rawCaptureId = Guid.Empty;

        if (job.Method.Name != "ProcessCaptureAsync" || job.Args.Count != 1)
        {
            return false;
        }

        return job.Args[0] switch
        {
            Guid value => (rawCaptureId = value) != Guid.Empty,
            string value when Guid.TryParse(value, out var parsed) => (rawCaptureId = parsed) != Guid.Empty,
            _ => false
        };
    }

    private void UpdateCaptureStatus(Guid rawCaptureId, CaptureStatus status, string? failureReason, string reason)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var capture = dbContext.RawCaptures.FirstOrDefault(item => item.Id == rawCaptureId);
        if (capture == null)
        {
            return;
        }

        capture.Status = status;
        capture.Metadata = UpdateMetadata(capture.Metadata, failureReason);
        dbContext.SaveChanges();

        _logger.LogInformation(
            "Capture {RawCaptureId} marked as {CaptureStatus} by Hangfire state transition ({Reason})",
            rawCaptureId,
            status,
            reason);
    }

    private static string? UpdateMetadata(string? metadata, string? failureReason)
    {
        var payload = ParseMetadata(metadata);

        if (string.IsNullOrWhiteSpace(failureReason))
        {
            payload.Remove(ProcessingErrorMetadataKey);
            return payload.Count == 0 ? null : JsonSerializer.Serialize(payload);
        }

        payload[ProcessingErrorMetadataKey] = failureReason;
        return JsonSerializer.Serialize(payload);
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
