using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.EntityFrameworkCore;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Worker;

public sealed class CaptureProcessingStateFilter : IApplyStateFilter
{
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
                UpdateCaptureStatus(rawCaptureId, CaptureStatus.Pending, "pending for retry");
                break;
            case FailedState:
                UpdateCaptureStatus(rawCaptureId, CaptureStatus.Failed, "failed permanently");
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

    private void UpdateCaptureStatus(Guid rawCaptureId, CaptureStatus status, string reason)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var capture = dbContext.RawCaptures.FirstOrDefault(item => item.Id == rawCaptureId);
        if (capture == null)
        {
            return;
        }

        capture.Status = status;
        dbContext.SaveChanges();

        _logger.LogInformation(
            "Capture {RawCaptureId} marked as {CaptureStatus} by Hangfire state transition ({Reason})",
            rawCaptureId,
            status,
            reason);
    }
}
