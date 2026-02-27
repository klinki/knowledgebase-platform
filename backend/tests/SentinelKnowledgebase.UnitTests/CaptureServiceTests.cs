using AwesomeAssertions;
using FluentValidation;
using NSubstitute;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContentProcessor _contentProcessor;
    private readonly IMonitoringService _monitoringService;
    private readonly CaptureService _service;
    
    public CaptureServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _contentProcessor = Substitute.For<IContentProcessor>();
        _monitoringService = Substitute.For<IMonitoringService>();
        _service = new CaptureService(_unitOfWork, _contentProcessor, _monitoringService);
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
        
        _unitOfWork.Tags.GetByNameAsync("test")
            .Returns((Tag?)null);
        
        _unitOfWork.Tags.AddAsync(Arg.Any<Tag>())
            .Returns(mockTag);
        
        _unitOfWork.RawCaptures.AddAsync(Arg.Any<RawCapture>())
            .Returns(callInfo => callInfo.Arg<RawCapture>());
        
        _unitOfWork.SaveChangesAsync()
            .Returns(1);
        
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
        _unitOfWork.RawCaptures.GetByIdAsync(nonexistentId)
            .Returns((RawCapture?)null);
        
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
        
        _unitOfWork.RawCaptures.GetByIdAsync(captureId)
            .Returns(rawCapture);
        
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
        
        _unitOfWork.RawCaptures.GetAllAsync()
            .Returns(captures);
        
        var result = await _service.GetAllCapturesAsync();
        
        result.Should().HaveCount(2);
    }
    
    [Fact]
    public async Task DeleteCaptureAsync_ShouldCallDelete()
    {
        var captureId = Guid.NewGuid();
        
        _unitOfWork.RawCaptures.DeleteAsync(captureId)
            .Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync()
            .Returns(1);
        
        await _service.DeleteCaptureAsync(captureId);
        
        await _unitOfWork.RawCaptures.Received(1).DeleteAsync(captureId);
    }
}
