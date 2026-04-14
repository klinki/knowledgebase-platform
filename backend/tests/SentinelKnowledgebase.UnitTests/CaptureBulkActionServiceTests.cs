using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SentinelKnowledgebase.Application.DTOs.Labels;
using SentinelKnowledgebase.Application.Services;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Repositories;
using Xunit;

namespace SentinelKnowledgebase.UnitTests;

public class CaptureBulkActionServiceTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRawCaptureRepository _rawCaptureRepository;
    private readonly ITagRepository _tagRepository;
    private readonly ILabelCategoryRepository _labelCategoryRepository;
    private readonly ILabelValueRepository _labelValueRepository;
    private readonly IProcessedInsightRepository _processedInsightRepository;
    private readonly CaptureBulkActionService _service;

    public CaptureBulkActionServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _rawCaptureRepository = Substitute.For<IRawCaptureRepository>();
        _tagRepository = Substitute.For<ITagRepository>();
        _labelCategoryRepository = Substitute.For<ILabelCategoryRepository>();
        _labelValueRepository = Substitute.For<ILabelValueRepository>();
        _processedInsightRepository = Substitute.For<IProcessedInsightRepository>();

        _unitOfWork.RawCaptures.Returns(_rawCaptureRepository);
        _unitOfWork.Tags.Returns(_tagRepository);
        _unitOfWork.LabelCategories.Returns(_labelCategoryRepository);
        _unitOfWork.LabelValues.Returns(_labelValueRepository);
        _unitOfWork.ProcessedInsights.Returns(_processedInsightRepository);
        _unitOfWork.SaveChangesAsync().Returns(1);

        var logger = Substitute.For<ILogger<CaptureBulkActionService>>();
        _service = new CaptureBulkActionService(_unitOfWork, logger);
    }

    [Fact]
    public async Task FindDeletedTweetsFromUnavailableAccountsAsync_ShouldReturnOnlySelectedSkipCodes()
    {
        var ownerUserId = Guid.NewGuid();
        var records = new List<CaptureMetadataRecord>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SourceUrl = "https://twitter.com/a",
                RawContent = "tweet-a",
                Metadata = """{"processingSkipCode":"twitter_suspended_account","processingSkipReason":"Suspended"}""",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                SourceUrl = "https://twitter.com/b",
                RawContent = "tweet-b",
                Metadata = """{"processingSkipCode":"twitter_account_limited","processingSkipReason":"Limited"}""",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                SourceUrl = "https://twitter.com/c",
                RawContent = "tweet-c",
                Metadata = """{"processingSkipCode":"twitter_post_unavailable","processingSkipReason":"Unavailable"}""",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                SourceUrl = "https://twitter.com/d",
                RawContent = "tweet-d",
                Metadata = """{"processingSkipCode":"other_skip_code","processingSkipReason":"Ignore"}""",
                CreatedAt = DateTime.UtcNow
            }
        };

        _rawCaptureRepository.GetCompletedTweetsWithMetadataAsync(ownerUserId, Arg.Any<int>())
            .Returns(records);

        var result = await _service.FindDeletedTweetsFromUnavailableAccountsAsync(ownerUserId, 5000, 20);

        result.TotalCount.Should().Be(3);
        result.CaptureIds.Should().BeEquivalentTo(records.Take(3).Select(record => record.Id));
        result.PreviewItems.Should().HaveCount(3);
    }

    [Fact]
    public async Task AddTagsAsync_ShouldMutateRawAndProcessedInsights()
    {
        var ownerUserId = Guid.NewGuid();
        var captureId = Guid.NewGuid();
        var capture = new RawCapture
        {
            Id = captureId,
            OwnerUserId = ownerUserId,
            SourceUrl = "https://example.com",
            RawContent = "raw",
            ProcessedInsight = new ProcessedInsight
            {
                Id = Guid.NewGuid(),
                OwnerUserId = ownerUserId,
                RawCaptureId = captureId,
                Title = "title",
                Summary = "summary"
            }
        };

        _rawCaptureRepository.GetByIdsWithGraphAsync(ownerUserId, Arg.Any<IReadOnlyCollection<Guid>>())
            .Returns([capture]);
        _tagRepository.GetAllAsync(ownerUserId).Returns([]);

        var result = await _service.AddTagsAsync(ownerUserId, [captureId], ["cleanup"]);

        result.MatchedCount.Should().Be(1);
        result.MutatedCount.Should().Be(1);
        capture.Tags.Should().ContainSingle(tag => tag.Name == "cleanup");
        capture.ProcessedInsight!.Tags.Should().ContainSingle(tag => tag.Name == "cleanup");
        await _unitOfWork.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task RemoveLabelsAsync_ShouldMutateRawAndProcessedInsights()
    {
        var ownerUserId = Guid.NewGuid();
        var captureId = Guid.NewGuid();
        var category = new LabelCategory
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Name = "Source"
        };
        var value = new LabelValue
        {
            Id = Guid.NewGuid(),
            LabelCategoryId = category.Id,
            LabelCategory = category,
            Value = "Twitter"
        };
        var capture = new RawCapture
        {
            Id = captureId,
            OwnerUserId = ownerUserId,
            SourceUrl = "https://twitter.com/i/web/status/1",
            RawContent = "tweet",
            LabelAssignments =
            [
                new RawCaptureLabelAssignment
                {
                    RawCaptureId = captureId,
                    LabelCategoryId = category.Id,
                    LabelCategory = category,
                    LabelValueId = value.Id,
                    LabelValue = value
                }
            ],
            ProcessedInsight = new ProcessedInsight
            {
                Id = Guid.NewGuid(),
                OwnerUserId = ownerUserId,
                RawCaptureId = captureId,
                Title = "title",
                Summary = "summary",
                LabelAssignments =
                [
                    new ProcessedInsightLabelAssignment
                    {
                        ProcessedInsightId = Guid.NewGuid(),
                        LabelCategoryId = category.Id,
                        LabelCategory = category,
                        LabelValueId = value.Id,
                        LabelValue = value
                    }
                ]
            }
        };

        _rawCaptureRepository.GetByIdsWithGraphAsync(ownerUserId, Arg.Any<IReadOnlyCollection<Guid>>())
            .Returns([capture]);

        var result = await _service.RemoveLabelsAsync(
            ownerUserId,
            [captureId],
            [new LabelAssignmentDto { Category = "Source", Value = "Twitter" }]);

        result.MatchedCount.Should().Be(1);
        result.MutatedCount.Should().Be(1);
        capture.LabelAssignments.Should().BeEmpty();
        capture.ProcessedInsight!.LabelAssignments.Should().BeEmpty();
        await _unitOfWork.Received(1).SaveChangesAsync();
    }
}
