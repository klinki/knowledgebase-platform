using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using SentinelKnowledgebase.Application.DTOs.Dashboard;
using SentinelKnowledgebase.Application.DTOs.Labels;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Application.Services;

public class CaptureProcessingAdminService : ICaptureProcessingAdminService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly JobStorage _jobStorage;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<CaptureProcessingAdminService> _logger;

    public CaptureProcessingAdminService(
        IUnitOfWork unitOfWork,
        JobStorage jobStorage,
        IBackgroundJobClient backgroundJobClient,
        ILogger<CaptureProcessingAdminService> logger)
    {
        _unitOfWork = unitOfWork;
        _jobStorage = jobStorage;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public async Task<bool> IsPausedAsync()
    {
        var control = await GetOrCreateControlAsync();
        return control.IsPaused;
    }

    public async Task<CaptureProcessingAdminOverviewDto> GetOverviewAsync()
    {
        var control = await GetOrCreateControlAsync();
        return await BuildOverviewAsync(control);
    }

    public async Task<CaptureProcessingAdminOverviewDto> PauseAsync(Guid changedByUserId)
    {
        var control = await GetOrCreateControlAsync();
        control.IsPaused = true;
        control.ChangedAt = DateTimeOffset.UtcNow;
        control.ChangedByUserId = changedByUserId;

        await _unitOfWork.CaptureProcessingControls.UpdateAsync(control);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Capture processing paused by user {ChangedByUserId}", changedByUserId);
        return await BuildOverviewAsync(control);
    }

    public async Task<CaptureProcessingAdminOverviewDto> ResumeAsync(Guid changedByUserId)
    {
        var control = await GetOrCreateControlAsync();
        control.IsPaused = false;
        control.ChangedAt = DateTimeOffset.UtcNow;
        control.ChangedByUserId = changedByUserId;

        await _unitOfWork.CaptureProcessingControls.UpdateAsync(control);
        await _unitOfWork.SaveChangesAsync();

        var pendingCaptureIds = await _unitOfWork.RawCaptures.GetPendingIdsAsync();
        foreach (var captureId in pendingCaptureIds)
        {
            _backgroundJobClient.Enqueue<ICaptureService>(service => service.ProcessCaptureAsync(captureId));
        }

        _logger.LogInformation(
            "Capture processing resumed by user {ChangedByUserId}; {PendingCaptureCount} pending captures enqueued",
            changedByUserId,
            pendingCaptureIds.Count);

        return await BuildOverviewAsync(control);
    }

    private async Task<CaptureProcessingAdminOverviewDto> BuildOverviewAsync(CaptureProcessingControl control)
    {
        var captureCounts = await _unitOfWork.RawCaptures.GetStatusCountsAsync();
        var recentCaptures = await _unitOfWork.RawCaptures.GetRecentGlobalAsync(10);
        var monitoringApi = _jobStorage.GetMonitoringApi();
        var queueNames = monitoringApi.Queues()
            .Select(queue => queue.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        long enqueuedCount = 0;
        foreach (var queueName in queueNames)
        {
            enqueuedCount += monitoringApi.EnqueuedCount(queueName);
        }

        string? changedByDisplayName = null;
        if (control.ChangedByUserId.HasValue)
        {
            changedByDisplayName = await _unitOfWork.CaptureProcessingControls.GetDisplayNameAsync(control.ChangedByUserId.Value);
        }

        return new CaptureProcessingAdminOverviewDto
        {
            IsPaused = control.IsPaused,
            ChangedAt = control.ChangedAt,
            ChangedByDisplayName = changedByDisplayName,
            CaptureCounts = new CaptureProcessingCaptureCountsDto
            {
                Pending = captureCounts.GetValueOrDefault(CaptureStatus.Pending),
                Processing = captureCounts.GetValueOrDefault(CaptureStatus.Processing),
                Completed = captureCounts.GetValueOrDefault(CaptureStatus.Completed),
                Failed = captureCounts.GetValueOrDefault(CaptureStatus.Failed)
            },
            JobCounts = new CaptureProcessingJobCountsDto
            {
                Enqueued = enqueuedCount,
                Scheduled = monitoringApi.ScheduledCount(),
                Processing = monitoringApi.ProcessingCount(),
                Failed = monitoringApi.FailedCount()
            },
            RecentCaptures = recentCaptures.Select(MapCapture).ToList()
        };
    }

    private async Task<CaptureProcessingControl> GetOrCreateControlAsync()
    {
        var control = await _unitOfWork.CaptureProcessingControls.GetAsync();
        if (control != null)
        {
            return control;
        }

        control = new CaptureProcessingControl();
        await _unitOfWork.CaptureProcessingControls.AddAsync(control);
        await _unitOfWork.SaveChangesAsync();
        return control;
    }

    private static CaptureListItemDto MapCapture(RawCapture capture)
    {
        return new CaptureListItemDto
        {
            Id = capture.Id,
            Title = capture.ProcessedInsight?.Title ?? capture.SourceUrl,
            SourceUrl = capture.SourceUrl,
            CapturedAt = capture.CreatedAt,
            Status = capture.Status,
            Tags = capture.Tags.Select(tag => tag.Name).ToList(),
            Labels = capture.LabelAssignments
                .OrderBy(assignment => assignment.LabelCategory.Name)
                .ThenBy(assignment => assignment.LabelValue.Value)
                .Select(assignment => new LabelAssignmentDto
                {
                    Category = assignment.LabelCategory.Name,
                    Value = assignment.LabelValue.Value
                })
                .ToList()
        };
    }
}
