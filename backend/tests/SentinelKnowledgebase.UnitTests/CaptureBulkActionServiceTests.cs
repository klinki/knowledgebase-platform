using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SentinelKnowledgebase.Application.DTOs.Labels;
using SentinelKnowledgebase.Application.Services;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
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
    private readonly IContentProcessor _contentProcessor;
    private readonly CaptureBulkActionService _service;

    public CaptureBulkActionServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _rawCaptureRepository = Substitute.For<IRawCaptureRepository>();
        _tagRepository = Substitute.For<ITagRepository>();
        _labelCategoryRepository = Substitute.For<ILabelCategoryRepository>();
        _labelValueRepository = Substitute.For<ILabelValueRepository>();
        _processedInsightRepository = Substitute.For<IProcessedInsightRepository>();
        _contentProcessor = Substitute.For<IContentProcessor>();

        _unitOfWork.RawCaptures.Returns(_rawCaptureRepository);
        _unitOfWork.Tags.Returns(_tagRepository);
        _unitOfWork.LabelCategories.Returns(_labelCategoryRepository);
        _unitOfWork.LabelValues.Returns(_labelValueRepository);
        _unitOfWork.ProcessedInsights.Returns(_processedInsightRepository);
        _unitOfWork.SaveChangesAsync().Returns(1);
        _contentProcessor.GenerateEmbeddingAsync(Arg.Any<string>())
            .Returns(callInfo => Task.FromResult(new[] { 0.42f }));

        var logger = Substitute.For<ILogger<CaptureBulkActionService>>();
        _service = new CaptureBulkActionService(_unitOfWork, _contentProcessor, logger);
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

    [Fact]
    public async Task SearchCapturesAsync_ShouldRequireAtLeastOneCriterion()
    {
        var ownerUserId = Guid.NewGuid();

        var act = async () => await _service.SearchCapturesAsync(ownerUserId, new CaptureSearchCriteria(), 5000, 20);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SearchCapturesAsync_ShouldApplyHybridCriteriaAndMapPreview()
    {
        var ownerUserId = Guid.NewGuid();
        var captureId = Guid.NewGuid();
        CaptureSearchQueryOptions? observedOptions = null;

        _rawCaptureRepository.SearchCapturesAsync(
                ownerUserId,
                Arg.Do<CaptureSearchQueryOptions>(options => observedOptions = options))
            .Returns(new CaptureSearchQueryResult
            {
                CaptureIds = [captureId],
                TotalCount = 1,
                Page = 2,
                PageSize = 10,
                Items =
                [
                    new CaptureSearchRecord
                    {
                        CaptureId = captureId,
                        SourceUrl = "https://example.com/capture",
                        RawContent = "Critical outage details in this capture body",
                        Metadata = """{"processingSkipCode":"twitter_post_unavailable","processingSkipReason":"Unavailable"}""",
                        ContentType = ContentType.Article,
                        Status = CaptureStatus.Failed,
                        CreatedAt = DateTime.UtcNow,
                        Similarity = 0.87,
                        MatchedByText = true,
                        MatchedBySemantic = true
                    }
                ]
            });

        var result = await _service.SearchCapturesAsync(
            ownerUserId,
            new CaptureSearchCriteria
            {
                Query = "critical outage",
                Tags = ["incident", "ops"],
                TagMatchMode = "all",
                Labels = [new LabelAssignmentDto { Category = "Team", Value = "SRE" }],
                LabelMatchMode = "any",
                Page = 2,
                PageSize = 10,
                Threshold = 0.55,
                ContentType = ContentType.Article,
                Status = CaptureStatus.Failed,
                DateFrom = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                DateTo = new DateTime(2026, 4, 14, 23, 59, 59, DateTimeKind.Utc)
            },
            maxResultSetSize: 5000,
            previewSize: 10);

        observedOptions.Should().NotBeNull();
        observedOptions!.Query.Should().Be("critical outage");
        observedOptions.MatchAllTags.Should().BeTrue();
        observedOptions.MatchAllLabels.Should().BeFalse();
        observedOptions.ContentType.Should().Be(ContentType.Article);
        observedOptions.Status.Should().Be(CaptureStatus.Failed);
        observedOptions.Page.Should().Be(2);
        observedOptions.PageSize.Should().Be(10);
        observedOptions.DateFrom.Should().NotBeNull();
        observedOptions.DateTo.Should().NotBeNull();

        result.TotalCount.Should().Be(1);
        result.CaptureIds.Should().ContainSingle().Which.Should().Be(captureId);
        result.PreviewItems.Should().ContainSingle();
        result.PreviewItems[0].MatchReason.Should().Be("semantic+text");
        result.PreviewItems[0].SkipCode.Should().Be("twitter_post_unavailable");
        await _contentProcessor.Received(1).GenerateEmbeddingAsync("critical outage");
    }
}
