using AwesomeAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SentinelKnowledgebase.Application.DTOs.Dashboard;
using SentinelKnowledgebase.Application.Services;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Repositories;
using Xunit;

namespace SentinelKnowledgebase.UnitTests;

public class CaptureProcessingAdminServiceTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly JobStorage _jobStorage;
    private readonly IMonitoringApi _monitoringApi;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<CaptureProcessingAdminService> _logger;
    private readonly CaptureProcessingAdminService _service;

    public CaptureProcessingAdminServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _jobStorage = Substitute.For<JobStorage>();
        _monitoringApi = Substitute.For<IMonitoringApi>();
        _backgroundJobClient = Substitute.For<IBackgroundJobClient>();
        _logger = Substitute.For<ILogger<CaptureProcessingAdminService>>();

        _jobStorage.GetMonitoringApi().Returns(_monitoringApi);
        _monitoringApi.Queues().Returns(
        [
            new QueueWithTopEnqueuedJobsDto
            {
                Name = "default",
                Length = 3
            }
        ]);
        _monitoringApi.EnqueuedCount("default").Returns(3);
        _monitoringApi.ScheduledCount().Returns(2);
        _monitoringApi.ProcessingCount().Returns(1);
        _monitoringApi.FailedCount().Returns(4);
        _backgroundJobClient.Create(Arg.Any<Job>(), Arg.Any<IState>()).Returns("job-1");
        _unitOfWork.RawCaptures.GetStatusCountsAsync().Returns(new Dictionary<CaptureStatus, int>());
        _unitOfWork.RawCaptures.GetRecentGlobalAsync(10).Returns([]);

        _service = new CaptureProcessingAdminService(
            _unitOfWork,
            _jobStorage,
            _backgroundJobClient,
            _logger);
    }

    [Fact]
    public async Task PauseAsync_ShouldUpdateControlWithActorAndTimestamp()
    {
        var changedByUserId = Guid.NewGuid();
        var control = new CaptureProcessingControl();
        _unitOfWork.CaptureProcessingControls.GetAsync().Returns(control);

        var overview = await _service.PauseAsync(changedByUserId);

        overview.IsPaused.Should().BeTrue();
        control.IsPaused.Should().BeTrue();
        control.ChangedByUserId.Should().Be(changedByUserId);
        control.ChangedAt.Should().NotBeNull();
        await _unitOfWork.CaptureProcessingControls.Received(1).UpdateAsync(control);
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task ResumeAsync_ShouldEnqueueAllPendingCaptures()
    {
        var control = new CaptureProcessingControl { IsPaused = true };
        var pendingIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        _unitOfWork.CaptureProcessingControls.GetAsync().Returns(control);
        _unitOfWork.RawCaptures.GetPendingIdsAsync().Returns(pendingIds);

        var overview = await _service.ResumeAsync(Guid.NewGuid());

        overview.IsPaused.Should().BeFalse();
        _backgroundJobClient.Received(2).Create(
            Arg.Any<Job>(),
            Arg.Is<IState>(state => state.Name == "Enqueued"));
    }

    [Fact]
    public async Task GetOverviewAsync_ShouldReturnCaptureAndJobCounts()
    {
        var control = new CaptureProcessingControl
        {
            IsPaused = true,
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedByUserId = Guid.NewGuid()
        };

        _unitOfWork.CaptureProcessingControls.GetAsync().Returns(control);
        _unitOfWork.CaptureProcessingControls.GetDisplayNameAsync(control.ChangedByUserId.Value)
            .Returns("Integration Admin");
        _unitOfWork.RawCaptures.GetStatusCountsAsync().Returns(new Dictionary<CaptureStatus, int>
        {
            [CaptureStatus.Pending] = 10,
            [CaptureStatus.Processing] = 2,
            [CaptureStatus.Completed] = 8,
            [CaptureStatus.Failed] = 1
        });
        _unitOfWork.RawCaptures.GetRecentGlobalAsync(10).Returns(
        [
            new RawCapture
            {
                Id = Guid.NewGuid(),
                SourceUrl = "https://example.com",
                CreatedAt = DateTime.UtcNow,
                Status = CaptureStatus.Pending,
                Tags = [],
                LabelAssignments = []
            }
        ]);

        var overview = await _service.GetOverviewAsync();

        overview.Should().BeEquivalentTo(new CaptureProcessingAdminOverviewDto
        {
            IsPaused = true,
            ChangedAt = control.ChangedAt,
            ChangedByDisplayName = "Integration Admin",
            CaptureCounts = new()
            {
                Pending = 10,
                Processing = 2,
                Completed = 8,
                Failed = 1
            },
            JobCounts = new()
            {
                Enqueued = 3,
                Scheduled = 2,
                Processing = 1,
                Failed = 4
            }
        }, options => options.Excluding(dto => dto.RecentCaptures));
        overview.RecentCaptures.Should().HaveCount(1);
        overview.RecentCaptures[0].SourceUrl.Should().Be("https://example.com");
    }
}
