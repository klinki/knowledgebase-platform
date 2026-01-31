using Moq;
using SentinelKnowledgebase.Application.DTOs;
using SentinelKnowledgebase.Application.Services;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Tests.Unit;

public class CaptureServiceTests
{
    private readonly Mock<IRawCaptureRepository> _captureRepositoryMock;
    private readonly Mock<ITagRepository> _tagRepositoryMock;
    private readonly CaptureService _captureService;

    public CaptureServiceTests()
    {
        _captureRepositoryMock = new Mock<IRawCaptureRepository>();
        _tagRepositoryMock = new Mock<ITagRepository>();
        _captureService = new CaptureService(_captureRepositoryMock.Object, _tagRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateCaptureAsync_WithValidRequest_ReturnsCaptureResponse()
    {
        // Arrange
        var request = new CaptureRequest
        {
            SourceUrl = "https://example.com",
            RawContent = "Test content",
            Source = CaptureSource.WebPage,
            Tags = new List<string> { "test" }
        };

        var expectedCapture = new RawCapture
        {
            Id = Guid.NewGuid(),
            SourceUrl = request.SourceUrl,
            RawContent = request.RawContent,
            Source = request.Source,
            Status = CaptureStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _tagRepositoryMock.Setup(x => x.GetByNameAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tag?)null);
        _tagRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tag { Name = "test" });
        _captureRepositoryMock.Setup(x => x.AddAsync(It.IsAny<RawCapture>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCapture);

        // Act
        var result = await _captureService.CreateCaptureAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCapture.Id, result.Id);
        Assert.Equal(expectedCapture.SourceUrl, result.SourceUrl);
        Assert.Equal(CaptureStatus.Pending, result.Status);
        _captureRepositoryMock.Verify(x => x.AddAsync(It.IsAny<RawCapture>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCaptureAsync_WithExistingTag_ReusesExistingTag()
    {
        // Arrange
        var request = new CaptureRequest
        {
            SourceUrl = "https://example.com",
            RawContent = "Test content",
            Source = CaptureSource.WebPage,
            Tags = new List<string> { "existing" }
        };

        var existingTag = new Tag { Id = Guid.NewGuid(), Name = "existing" };

        _tagRepositoryMock.Setup(x => x.GetByNameAsync("existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTag);
        _captureRepositoryMock.Setup(x => x.AddAsync(It.IsAny<RawCapture>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RawCapture { Id = Guid.NewGuid() });

        // Act
        await _captureService.CreateCaptureAsync(request);

        // Assert
        _tagRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetInsightAsync_WithExistingInsight_ReturnsInsightResponse()
    {
        // Arrange
        var captureId = Guid.NewGuid();
        var insightId = Guid.NewGuid();

        var capture = new RawCapture
        {
            Id = captureId,
            SourceUrl = "https://example.com",
            ProcessedInsight = new ProcessedInsight
            {
                Id = insightId,
                RawCaptureId = captureId,
                Title = "Test Title",
                Summary = "Test Summary",
                CleanContent = "Test Content",
                ProcessedAt = DateTime.UtcNow
            },
            Tags = new List<Tag> { new Tag { Name = "test" } }
        };

        _captureRepositoryMock.Setup(x => x.GetByIdAsync(captureId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(capture);

        // Act
        var result = await _captureService.GetInsightAsync(captureId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(insightId, result.Id);
        Assert.Equal("Test Title", result.Title);
        Assert.Single(result.Tags);
    }

    [Fact]
    public async Task GetInsightAsync_WithoutProcessedInsight_ReturnsNull()
    {
        // Arrange
        var captureId = Guid.NewGuid();

        var capture = new RawCapture
        {
            Id = captureId,
            SourceUrl = "https://example.com",
            ProcessedInsight = null
        };

        _captureRepositoryMock.Setup(x => x.GetByIdAsync(captureId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(capture);

        // Act
        var result = await _captureService.GetInsightAsync(captureId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetInsightAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var captureId = Guid.NewGuid();

        _captureRepositoryMock.Setup(x => x.GetByIdAsync(captureId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RawCapture?)null);

        // Act
        var result = await _captureService.GetInsightAsync(captureId);

        // Assert
        Assert.Null(result);
    }
}
