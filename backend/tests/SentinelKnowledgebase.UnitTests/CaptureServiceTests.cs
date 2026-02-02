using FluentAssertions;
using FluentValidation;
using Moq;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Application.Services;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Repositories;
using Xunit;

namespace SentinelKnowledgebase.UnitTests;

public class CaptureServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IContentProcessor> _mockContentProcessor;
    private readonly CaptureService _service;
    
    public CaptureServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockContentProcessor = new Mock<IContentProcessor>();
        _service = new CaptureService(_mockUnitOfWork.Object, _mockContentProcessor.Object);
    }
    
    [Fact]
    public async Task CreateCaptureAsync_ShouldReturnCaptureResponse()
    {
        var request = new CaptureRequestDto
        {
            SourceUrl = "https://example.com/article",
            ContentType = ContentType.Article,
            RawContent = "Test content",
            Tags = new List<string> { "test" }
        };
        
        var mockTag = new Tag { Id = Guid.NewGuid(), Name = "test" };
        
        _mockUnitOfWork.Setup(x => x.Tags.GetByNameAsync("test"))
            .ReturnsAsync((Tag?)null);
        
        _mockUnitOfWork.Setup(x => x.Tags.AddAsync(It.IsAny<Tag>()))
            .ReturnsAsync(mockTag);
        
        _mockUnitOfWork.Setup(x => x.RawCaptures.AddAsync(It.IsAny<RawCapture>()))
            .ReturnsAsync((RawCapture rc) => rc);
        
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);
        
        var result = await _service.CreateCaptureAsync(request);
        
        result.Should().NotBeNull();
        result.SourceUrl.Should().Be(request.SourceUrl);
        result.ContentType.Should().Be(request.ContentType);
        result.Status.Should().Be(CaptureStatus.Pending);
    }
    
    [Fact]
    public async Task GetCaptureByIdAsync_ShouldReturnNullForNonexistent()
    {
        var nonexistentId = Guid.NewGuid();
        _mockUnitOfWork.Setup(x => x.RawCaptures.GetByIdAsync(nonexistentId))
            .ReturnsAsync((RawCapture?)null);
        
        var result = await _service.GetCaptureByIdAsync(nonexistentId);
        
        result.Should().BeNull();
    }
    
    [Fact]
    public async Task GetCaptureByIdAsync_ShouldReturnCaptureResponse()
    {
        var captureId = Guid.NewGuid();
        var rawCapture = new RawCapture
        {
            Id = captureId,
            SourceUrl = "https://example.com",
            ContentType = ContentType.Article,
            RawContent = "Test",
            Status = CaptureStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            Tags = new List<Tag>()
        };
        
        _mockUnitOfWork.Setup(x => x.RawCaptures.GetByIdAsync(captureId))
            .ReturnsAsync(rawCapture);
        
        var result = await _service.GetCaptureByIdAsync(captureId);
        
        result.Should().NotBeNull();
        result!.Id.Should().Be(captureId);
        result.SourceUrl.Should().Be("https://example.com");
    }
    
    [Fact]
    public async Task GetAllCapturesAsync_ShouldReturnAllCaptures()
    {
        var captures = new List<RawCapture>
        {
            new() { Id = Guid.NewGuid(), SourceUrl = "https://example1.com", ContentType = ContentType.Article, Status = CaptureStatus.Pending, Tags = new List<Tag>() },
            new() { Id = Guid.NewGuid(), SourceUrl = "https://example2.com", ContentType = ContentType.Tweet, Status = CaptureStatus.Completed, Tags = new List<Tag>() }
        };
        
        _mockUnitOfWork.Setup(x => x.RawCaptures.GetAllAsync())
            .ReturnsAsync(captures);
        
        var result = await _service.GetAllCapturesAsync();
        
        result.Should().HaveCount(2);
    }
    
    [Fact]
    public async Task DeleteCaptureAsync_ShouldCallDelete()
    {
        var captureId = Guid.NewGuid();
        
        _mockUnitOfWork.Setup(x => x.RawCaptures.DeleteAsync(captureId))
            .Verifiable();
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);
        
        await _service.DeleteCaptureAsync(captureId);
        
        _mockUnitOfWork.Verify(x => x.RawCaptures.DeleteAsync(captureId), Times.Once);
    }
}
