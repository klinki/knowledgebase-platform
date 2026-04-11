using AwesomeAssertions;
using System.Text.Json;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Application.Hangfire;
using SentinelKnowledgebase.Application.DTOs.Labels;
using SentinelKnowledgebase.Application.Services;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Domain.Services;
using SentinelKnowledgebase.Infrastructure.Repositories;
using Xunit;

namespace SentinelKnowledgebase.UnitTests;

public class CaptureServiceTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContentProcessor _contentProcessor;
    private readonly IUserLanguagePreferencesService _userLanguagePreferencesService;
    private readonly IMonitoringService _monitoringService;
    private readonly IInsightClusteringService _insightClusteringService;
    private readonly ICaptureProcessingAdminService _captureProcessingAdminService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<CaptureService> _logger;
    private readonly CaptureService _service;
    
    public CaptureServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _contentProcessor = Substitute.For<IContentProcessor>();
        _userLanguagePreferencesService = Substitute.For<IUserLanguagePreferencesService>();
        _monitoringService = Substitute.For<IMonitoringService>();
        _insightClusteringService = Substitute.For<IInsightClusteringService>();
        _captureProcessingAdminService = Substitute.For<ICaptureProcessingAdminService>();
        _backgroundJobClient = Substitute.For<IBackgroundJobClient>();
        _logger = Substitute.For<ILogger<CaptureService>>();
        _service = new CaptureService(
            _unitOfWork,
            _contentProcessor,
            _userLanguagePreferencesService,
            _monitoringService,
            _insightClusteringService,
            _captureProcessingAdminService,
            _backgroundJobClient,
            _logger);

        _unitOfWork.Tags.GetAllAsync(Arg.Any<Guid>())
            .Returns(Task.FromResult<IEnumerable<Tag>>([]));
        _unitOfWork.LabelCategories.GetAllWithValuesAsync(Arg.Any<Guid>())
            .Returns(Task.FromResult<IEnumerable<LabelCategory>>([]));
        _unitOfWork.RawCaptures.AddAsync(Arg.Any<RawCapture>())
            .Returns(callInfo => callInfo.Arg<RawCapture>());
        _unitOfWork.SaveChangesAsync()
            .Returns(1);
        _captureProcessingAdminService.IsPausedAsync()
            .Returns(false);
        _userLanguagePreferencesService.GetAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new UserLanguagePreferencesSnapshot
            {
                DefaultLanguageCode = "en",
                PreservedLanguageCodes = []
            });
        _backgroundJobClient.Create(Arg.Any<Job>(), Arg.Any<IState>())
            .Returns("job-1");
    }
    
    [Fact]
    public async Task CreateCaptureAsync_ShouldReturnCaptureResponse()
    {
        var ownerUserId = Guid.NewGuid();
        var request = new CaptureRequestDto
        {
            SourceUrl = "https://example.com/article",
            ContentType = ContentType.Article,
            RawContent = "Test content",
            Tags = new List<string> { "test" }
        };
        
        var result = await _service.CreateCaptureAsync(ownerUserId, request);
        
        result.Should().NotBeNull();
        result.SourceUrl.Should().Be(request.SourceUrl);
        result.ContentType.Should().Be(request.ContentType);
        result.Status.Should().Be(CaptureStatus.Pending);
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task CreateCapturesAsync_ShouldPersistBatchOnceAndReuseExistingTagsAndLabels()
    {
        var ownerUserId = Guid.NewGuid();
        var existingTag = new Tag { Id = Guid.NewGuid(), OwnerUserId = ownerUserId, Name = "existing-tag" };
        var existingCategory = new LabelCategory
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Name = "Source",
            Values =
            [
                new LabelValue
                {
                    Id = Guid.NewGuid(),
                    LabelCategoryId = Guid.Empty,
                    Value = "Twitter"
                }
            ]
        };
        existingCategory.Values[0].LabelCategoryId = existingCategory.Id;
        existingCategory.Values[0].LabelCategory = existingCategory;

        _unitOfWork.Tags.GetAllAsync(ownerUserId)
            .Returns(Task.FromResult<IEnumerable<Tag>>([existingTag]));
        _unitOfWork.LabelCategories.GetAllWithValuesAsync(ownerUserId)
            .Returns(Task.FromResult<IEnumerable<LabelCategory>>([existingCategory]));

        var requests = new List<CaptureRequestDto>
        {
            new()
            {
                SourceUrl = "https://example.com/1",
                ContentType = ContentType.Article,
                RawContent = "One",
                Tags = ["existing-tag", "new-tag"],
                Labels = [new LabelAssignmentDto { Category = "Source", Value = "Twitter" }]
            },
            new()
            {
                SourceUrl = "https://example.com/2",
                ContentType = ContentType.Article,
                RawContent = "Two",
                Tags = ["new-tag"],
                Labels = [new LabelAssignmentDto { Category = "Source", Value = "Twitter" }]
            }
        };

        var result = await _service.CreateCapturesAsync(ownerUserId, requests);

        result.Should().HaveCount(2);
        await _unitOfWork.Received(1).SaveChangesAsync();
        await _unitOfWork.Tags.Received(1).AddAsync(Arg.Is<Tag>(tag => tag.Name == "new-tag"));
        await _unitOfWork.LabelCategories.DidNotReceive().AddAsync(Arg.Any<LabelCategory>());
        await _unitOfWork.LabelValues.DidNotReceive().AddAsync(Arg.Any<LabelValue>());
    }

    [Fact]
    public async Task CreateCaptureAsync_ShouldAutoFillSourceAndLanguageLabels()
    {
        var ownerUserId = Guid.NewGuid();
        var request = new CaptureRequestDto
        {
            SourceUrl = "https://example.com/article",
            ContentType = ContentType.Article,
            RawContent = "Test content",
            Metadata = """{"source":"webpage","metadata":{"language":"en-US"}}"""
        };

        var result = await _service.CreateCaptureAsync(ownerUserId, request);

        result.Labels.Should().Contain(label => label.Category == "Source" && label.Value == "Web");
        result.Labels.Should().Contain(label => label.Category == "Language" && label.Value == "English");
    }

    [Fact]
    public async Task CreateCaptureAsync_ShouldPreferExplicitLabelsOverAutoFilledLabels()
    {
        var ownerUserId = Guid.NewGuid();
        var request = new CaptureRequestDto
        {
            SourceUrl = "https://twitter.com/test/status/1",
            ContentType = ContentType.Tweet,
            RawContent = "Test tweet",
            Metadata = """{"source":"twitter"}""",
            Labels =
            [
                new LabelAssignmentDto { Category = "Source", Value = "Custom" }
            ]
        };

        var result = await _service.CreateCaptureAsync(ownerUserId, request);

        result.Labels.Should().ContainSingle(label => label.Category == "Source");
        result.Labels.Should().Contain(label => label.Category == "Source" && label.Value == "Custom");
    }
    
    [Fact]
    public async Task GetCaptureByIdAsync_ShouldReturnNullForNonexistent()
    {
        var ownerUserId = Guid.NewGuid();
        var nonexistentId = Guid.NewGuid();
        _unitOfWork.RawCaptures.GetByIdAsync(nonexistentId, ownerUserId)
            .Returns((RawCapture?)null);
        
        var result = await _service.GetCaptureByIdAsync(ownerUserId, nonexistentId);
        
        result.Should().BeNull();
    }
    
    [Fact]
    public async Task GetCaptureByIdAsync_ShouldReturnCaptureResponse()
    {
        var ownerUserId = Guid.NewGuid();
        var captureId = Guid.NewGuid();
        var rawCapture = new RawCapture
        {
            Id = captureId,
            OwnerUserId = ownerUserId,
            SourceUrl = "https://example.com",
            ContentType = ContentType.Article,
            RawContent = "Test",
            Status = CaptureStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            Tags = new List<Tag>()
        };
        
        _unitOfWork.RawCaptures.GetByIdAsync(captureId, ownerUserId)
            .Returns(rawCapture);
        
        var result = await _service.GetCaptureByIdAsync(ownerUserId, captureId);
        
        result.Should().NotBeNull();
        result!.Id.Should().Be(captureId);
        result.SourceUrl.Should().Be("https://example.com");
    }
    
    [Fact]
    public async Task GetAllCapturesAsync_ShouldReturnAllCaptures()
    {
        var ownerUserId = Guid.NewGuid();
        var captures = new List<RawCapture>
        {
            new() { Id = Guid.NewGuid(), OwnerUserId = ownerUserId, SourceUrl = "https://example1.com", ContentType = ContentType.Article, Status = CaptureStatus.Pending, Tags = new List<Tag>() },
            new() { Id = Guid.NewGuid(), OwnerUserId = ownerUserId, SourceUrl = "https://example2.com", ContentType = ContentType.Tweet, Status = CaptureStatus.Completed, Tags = new List<Tag>() }
        };
        
        _unitOfWork.RawCaptures.GetAllAsync(ownerUserId)
            .Returns(captures);
        
        var result = await _service.GetAllCapturesAsync(ownerUserId);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCaptureListPageAsync_ShouldNormalizeQueryAndMapPagedResult()
    {
        var ownerUserId = Guid.NewGuid();
        CaptureListQueryOptions? capturedOptions = null;
        var createdAt = new DateTime(2026, 3, 31, 10, 15, 0, DateTimeKind.Utc);

        _unitOfWork.RawCaptures.GetPagedListAsync(ownerUserId, Arg.Any<CaptureListQueryOptions>())
            .Returns(callInfo =>
            {
                capturedOptions = callInfo.Arg<CaptureListQueryOptions>();
                return Task.FromResult(new CaptureListQueryResult
                {
                    TotalCount = 37,
                    Items =
                    [
                        new CaptureListRecord
                        {
                            Id = Guid.NewGuid(),
                            SourceUrl = "https://example.com/article",
                            ContentType = ContentType.Article,
                            Status = CaptureStatus.Failed,
                            CreatedAt = createdAt,
                            ProcessedAt = null,
                            Metadata = """{"lastProcessingError":"processor exploded"}"""
                        }
                    ]
                });
            });

        var result = await _service.GetCaptureListPageAsync(ownerUserId, new CaptureListQueryDto
        {
            Page = 0,
            PageSize = 999,
            SortField = "sourceUrl",
            SortDirection = "ASC",
            ContentType = "article",
            Status = "failed"
        });

        capturedOptions.Should().NotBeNull();
        capturedOptions!.Page.Should().Be(1);
        capturedOptions.PageSize.Should().Be(200);
        capturedOptions.SortField.Should().Be("sourceUrl");
        capturedOptions.SortDirection.Should().Be("asc");
        capturedOptions.ContentType.Should().Be(ContentType.Article);
        capturedOptions.Status.Should().Be(CaptureStatus.Failed);

        result.TotalCount.Should().Be(37);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(200);
        result.Items.Should().ContainSingle();
        result.Items[0].SourceUrl.Should().Be("https://example.com/article");
        result.Items[0].FailureReason.Should().Be("processor exploded");
    }

    [Fact]
    public async Task GetCaptureListPageAsync_ShouldThrowArgumentException_WhenStatusFilterInvalid()
    {
        var ownerUserId = Guid.NewGuid();

        var action = async () => await _service.GetCaptureListPageAsync(ownerUserId, new CaptureListQueryDto
        {
            Status = "not-a-status"
        });

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid status filter.*");
        await _unitOfWork.RawCaptures.DidNotReceive()
            .GetPagedListAsync(Arg.Any<Guid>(), Arg.Any<CaptureListQueryOptions>());
    }
 

    [Fact]
    public async Task RetryCaptureAsync_ShouldReturnFalse_WhenCaptureIsCompleted()
    {
        var ownerUserId = Guid.NewGuid();
        var captureId = Guid.NewGuid();
        var completedCapture = new RawCapture
        {
            Id = captureId,
            OwnerUserId = ownerUserId,
            SourceUrl = "https://example.com",
            ContentType = ContentType.Article,
            RawContent = "Test",
            Status = CaptureStatus.Completed,
            ProcessedAt = DateTime.UtcNow
        };

        _unitOfWork.RawCaptures.GetByIdAsync(captureId, ownerUserId)
            .Returns(completedCapture);

        var retried = await _service.RetryCaptureAsync(ownerUserId, captureId);

        retried.Should().BeFalse();
        await _unitOfWork.RawCaptures.DidNotReceive().UpdateAsync(Arg.Any<RawCapture>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task RetryFailedCapturesAsync_ShouldResetOnlyFailedCaptures_FromSelection()
    {
        var ownerUserId = Guid.NewGuid();
        var failedCaptureId = Guid.NewGuid();
        var pendingCaptureId = Guid.NewGuid();
        var failedCapture = new RawCapture
        {
            Id = failedCaptureId,
            OwnerUserId = ownerUserId,
            SourceUrl = "https://example.com/failed",
            ContentType = ContentType.Article,
            RawContent = "Failed content",
            Status = CaptureStatus.Failed,
            ProcessedAt = DateTime.UtcNow,
            Metadata = """{"lastProcessingError":"boom"}"""
        };
        var pendingCapture = new RawCapture
        {
            Id = pendingCaptureId,
            OwnerUserId = ownerUserId,
            SourceUrl = "https://example.com/pending",
            ContentType = ContentType.Article,
            RawContent = "Pending content",
            Status = CaptureStatus.Pending
        };

        _unitOfWork.RawCaptures.GetByIdsAsync(ownerUserId, Arg.Any<IReadOnlyCollection<Guid>>())
            .Returns([failedCapture, pendingCapture]);

        var retriedIds = await _service.RetryFailedCapturesAsync(ownerUserId, [failedCaptureId, pendingCaptureId]);

        retriedIds.Should().Equal([failedCaptureId]);
        failedCapture.Status.Should().Be(CaptureStatus.Pending);
        failedCapture.ProcessedAt.Should().BeNull();
        failedCapture.Metadata.Should().BeNull();
        pendingCapture.Status.Should().Be(CaptureStatus.Pending);
        await _unitOfWork.RawCaptures.Received(1).UpdateAsync(failedCapture);
        await _unitOfWork.RawCaptures.DidNotReceive().UpdateAsync(pendingCapture);
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task RetryAllFailedCapturesAsync_ShouldRespectRetryScopeStatusFilter()
    {
        var ownerUserId = Guid.NewGuid();

        var retriedIds = await _service.RetryAllFailedCapturesAsync(ownerUserId, new CaptureBulkRetryRequestDto
        {
            RetryAllMatching = true,
            Status = "Completed"
        });

        retriedIds.Should().BeEmpty();
        await _unitOfWork.RawCaptures.DidNotReceive().GetFailedAsync(Arg.Any<Guid>(), Arg.Any<ContentType?>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteCaptureAsync_ShouldCallDelete()
    {
        var ownerUserId = Guid.NewGuid();
        var captureId = Guid.NewGuid();
        var existingCapture = new RawCapture
        {
            Id = captureId,
            OwnerUserId = ownerUserId,
            SourceUrl = "https://example.com",
            ContentType = ContentType.Article,
            RawContent = "Test",
            Status = CaptureStatus.Pending
        };

        _unitOfWork.RawCaptures.GetByIdAsync(captureId, ownerUserId)
            .Returns(existingCapture);
        _unitOfWork.RawCaptures.DeleteAsync(captureId, ownerUserId)
            .Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync()
            .Returns(1);

        var deleted = await _service.DeleteCaptureAsync(ownerUserId, captureId);

        deleted.Should().BeTrue();
        await _unitOfWork.RawCaptures.Received(1).DeleteAsync(captureId, ownerUserId);
    }

    [Fact]
    public async Task DeleteCaptureAsync_ShouldReturnFalse_WhenCaptureDoesNotExist()
    {
        var ownerUserId = Guid.NewGuid();
        var captureId = Guid.NewGuid();

        _unitOfWork.RawCaptures.GetByIdAsync(captureId, ownerUserId)
            .Returns((RawCapture?)null);

        var deleted = await _service.DeleteCaptureAsync(ownerUserId, captureId);

        deleted.Should().BeFalse();
        await _unitOfWork.RawCaptures.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task ProcessCaptureAsync_ShouldCopyRawLabelsToProcessedInsight()
    {
        var ownerUserId = Guid.NewGuid();
        var rawCaptureId = Guid.NewGuid();
        var category = new LabelCategory
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Name = "Language"
        };
        var value = new LabelValue
        {
            Id = Guid.NewGuid(),
            LabelCategoryId = category.Id,
            LabelCategory = category,
            Value = "English"
        };
        var rawCapture = new RawCapture
        {
            Id = rawCaptureId,
            OwnerUserId = ownerUserId,
            SourceUrl = "https://example.com",
            ContentType = ContentType.Article,
            RawContent = "Test content",
            LabelAssignments =
            [
                new RawCaptureLabelAssignment
                {
                    RawCaptureId = rawCaptureId,
                    LabelCategoryId = category.Id,
                    LabelCategory = category,
                    LabelValueId = value.Id,
                    LabelValue = value
                }
            ]
        };

        _unitOfWork.RawCaptures.GetByIdAsync(rawCaptureId).Returns(rawCapture);
        _contentProcessor.DenoiseContent(rawCapture.RawContent).Returns(rawCapture.RawContent);
        _contentProcessor.ExtractInsightsAsync(rawCapture.RawContent, rawCapture.ContentType, Arg.Any<string?>())
            .Returns(new ContentInsights
            {
                Title = "Processed title",
                Summary = "Processed summary",
                KeyInsights = ["Key insight"],
                ActionItems = ["Action item"],
                SourceTitle = "Source title",
                Author = "Author"
            });
        _contentProcessor.GenerateEmbeddingAsync("Processed summary").Returns([0.1f, 0.2f, 0.3f]);

        await _service.ProcessCaptureAsync(rawCaptureId);

        await _unitOfWork.ProcessedInsights.Received(1).AddAsync(
            Arg.Is<ProcessedInsight>(insight =>
                insight.LabelAssignments.Count == 1 &&
                insight.LabelAssignments[0].LabelCategoryId == category.Id &&
                insight.LabelAssignments[0].LabelValueId == value.Id));
        _backgroundJobClient.Received(1).Create(
            Arg.Is<Job>(job =>
                job.Type == typeof(IInsightClusteringService) &&
                job.Method.Name == nameof(IInsightClusteringService.RebuildOwnerClustersAsync)),
            Arg.Is<IState>(state => IsClusteringQueueState(state)));
    }

    [Fact]
    public async Task ProcessCaptureAsync_ShouldSkipImmediateClustering_WhenMetadataDefersIt()
    {
        var ownerUserId = Guid.NewGuid();
        var rawCaptureId = Guid.NewGuid();
        var rawCapture = new RawCapture
        {
            Id = rawCaptureId,
            OwnerUserId = ownerUserId,
            SourceUrl = "https://twitter.com/i/web/status/123",
            ContentType = ContentType.Tweet,
            RawContent = "Imported tweet",
            Metadata = """{"source":"twitter","tweetId":"123","deferClustering":true}"""
        };

        _unitOfWork.RawCaptures.GetByIdAsync(rawCaptureId).Returns(rawCapture);
        _contentProcessor.DenoiseContent(rawCapture.RawContent).Returns(rawCapture.RawContent);
        _contentProcessor.ExtractInsightsAsync(rawCapture.RawContent, rawCapture.ContentType, Arg.Any<string?>())
            .Returns(new ContentInsights
            {
                Title = "Processed title",
                Summary = "Processed summary",
                KeyInsights = ["Key insight"],
                ActionItems = ["Action item"],
                SourceTitle = "Source title",
                Author = "Author"
            });
        _contentProcessor.GenerateEmbeddingAsync("Processed summary").Returns([0.1f, 0.2f, 0.3f]);

        await _service.ProcessCaptureAsync(rawCaptureId);

        using var metadataDocument = JsonDocument.Parse(rawCapture.Metadata!);
        metadataDocument.RootElement.TryGetProperty("deferClustering", out _).Should().BeFalse();
        metadataDocument.RootElement.GetProperty("source").GetString().Should().Be("twitter");
        metadataDocument.RootElement.GetProperty("tweetId").GetString().Should().Be("123");
        _backgroundJobClient.DidNotReceive().Create(
            Arg.Is<Job>(job =>
                job.Type == typeof(IInsightClusteringService) &&
                job.Method.Name == nameof(IInsightClusteringService.RebuildOwnerClustersAsync)),
            Arg.Any<IState>());
    }

    private static bool IsClusteringQueueState(IState state)
    {
        return state is EnqueuedState enqueuedState && enqueuedState.Queue == HangfireQueues.Clustering;
    }

    [Fact]
    public async Task ProcessCaptureAsync_ShouldScheduleRetryAndLeavePending_WhenPaused()
    {
        var rawCaptureId = Guid.NewGuid();
        var rawCapture = new RawCapture
        {
            Id = rawCaptureId,
            OwnerUserId = Guid.NewGuid(),
            SourceUrl = "https://example.com",
            ContentType = ContentType.Article,
            RawContent = "Pending content",
            Status = CaptureStatus.Pending
        };

        _unitOfWork.RawCaptures.GetByIdAsync(rawCaptureId).Returns(rawCapture);
        _captureProcessingAdminService.IsPausedAsync().Returns(true);

        await _service.ProcessCaptureAsync(rawCaptureId);

        await _unitOfWork.RawCaptures.DidNotReceive().UpdateAsync(Arg.Is<RawCapture>(capture => capture.Status == CaptureStatus.Processing));
        await _unitOfWork.ProcessedInsights.DidNotReceive().AddAsync(Arg.Any<ProcessedInsight>());
        _backgroundJobClient.Received(1).Create(
            Arg.Any<Job>(),
            Arg.Is<IState>(state => state is ScheduledState));
    }

    [Theory]
    [InlineData(CaptureStatus.Completed)]
    [InlineData(CaptureStatus.Processing)]
    public async Task ProcessCaptureAsync_ShouldSkip_WhenCaptureAlreadyHandled(CaptureStatus status)
    {
        var rawCaptureId = Guid.NewGuid();
        var rawCapture = new RawCapture
        {
            Id = rawCaptureId,
            OwnerUserId = Guid.NewGuid(),
            SourceUrl = "https://example.com",
            ContentType = ContentType.Article,
            RawContent = "Skipped content",
            Status = status
        };

        _unitOfWork.RawCaptures.GetByIdAsync(rawCaptureId).Returns(rawCapture);

        await _service.ProcessCaptureAsync(rawCaptureId);

        await _unitOfWork.RawCaptures.DidNotReceive().UpdateAsync(Arg.Any<RawCapture>());
        await _unitOfWork.ProcessedInsights.DidNotReceive().AddAsync(Arg.Any<ProcessedInsight>());
        _backgroundJobClient.DidNotReceive().Create(Arg.Any<Job>(), Arg.Any<IState>());
    }
}

