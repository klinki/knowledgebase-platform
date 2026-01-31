using Moq;
using SentinelKnowledgebase.Application.DTOs;
using SentinelKnowledgebase.Application.Services;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Tests.Unit;

public class SearchServiceTests
{
    private readonly Mock<IProcessedInsightRepository> _insightRepositoryMock;
    private readonly Mock<ITagRepository> _tagRepositoryMock;
    private readonly SearchService _searchService;

    public SearchServiceTests()
    {
        _insightRepositoryMock = new Mock<IProcessedInsightRepository>();
        _tagRepositoryMock = new Mock<ITagRepository>();
        _searchService = new SearchService(_insightRepositoryMock.Object, _tagRepositoryMock.Object);
    }

    [Fact]
    public async Task SearchByTagAsync_WithMatchingTag_ReturnsResults()
    {
        // Arrange
        var request = new TagSearchRequest { Tag = "important" };
        var captures = new List<RawCapture>
        {
            new RawCapture
            {
                Id = Guid.NewGuid(),
                SourceUrl = "https://example.com/1",
                Status = CaptureStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                ProcessedInsight = new ProcessedInsight
                {
                    Id = Guid.NewGuid(),
                    Title = "Title 1",
                    Summary = "Summary 1"
                },
                Tags = new List<Tag> { new Tag { Name = "important" } }
            }
        };

        _tagRepositoryMock.Setup(x => x.GetCapturesByTagAsync("important", It.IsAny<CancellationToken>()))
            .ReturnsAsync(captures);

        // Act
        var results = await _searchService.SearchByTagAsync(request);

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal("Title 1", results.First().Title);
    }

    [Fact]
    public async Task SearchByTagAsync_WithNoMatchingTag_ReturnsEmpty()
    {
        // Arrange
        var request = new TagSearchRequest { Tag = "nonexistent" };

        _tagRepositoryMock.Setup(x => x.GetCapturesByTagAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<RawCapture>());

        // Act
        var results = await _searchService.SearchByTagAsync(request);

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchByTagAsync_FiltersPendingCaptures()
    {
        // Arrange
        var request = new TagSearchRequest { Tag = "test" };
        var captures = new List<RawCapture>
        {
            new RawCapture
            {
                Id = Guid.NewGuid(),
                Status = CaptureStatus.Pending, // Should be filtered out
                ProcessedInsight = null,
                Tags = new List<Tag> { new Tag { Name = "test" } }
            }
        };

        _tagRepositoryMock.Setup(x => x.GetCapturesByTagAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(captures);

        // Act
        var results = await _searchService.SearchByTagAsync(request);

        // Assert
        Assert.Empty(results);
    }
}
