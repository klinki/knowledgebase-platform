using AwesomeAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Application.DTOs.Labels;
using SentinelKnowledgebase.Application.Services;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Repositories;
using Xunit;

namespace SentinelKnowledgebase.UnitTests;

public class CaptureServiceTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContentProcessor _contentProcessor;
    private readonly IMonitoringService _monitoringService;
    private readonly ICaptureProcessingAdminService _captureProcessingAdminService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<CaptureService> _logger;
    private readonly CaptureService _service;
    
    public CaptureServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _contentProcessor = Substitute.For<IContentProcessor>();
        _monitoringService = Substitute.For<IMonitoringService>();
        _captureProcessingAdminService = Substitute.For<ICaptureProcessingAdminService>();
        _backgroundJobClient = Substitute.For<IBackgroundJobClient>();
        _logger = Substitute.For<ILogger<CaptureService>>();
        _service = new CaptureService(
            _unitOfWork,
            _contentProcessor,
            _monitoringService,
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
        _contentProcessor.ExtractInsightsAsync(rawCapture.RawContent, rawCapture.ContentType)
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

