using AwesomeAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<CaptureService> _logger;
    private readonly CaptureService _service;
    
    public CaptureServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _contentProcessor = Substitute.For<IContentProcessor>();
        _monitoringService = Substitute.For<IMonitoringService>();
        _logger = Substitute.For<ILogger<CaptureService>>();
        _service = new CaptureService(_unitOfWork, _contentProcessor, _monitoringService, _logger);
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
        
        var mockTag = new Tag { Id = Guid.NewGuid(), Name = "test" };
        
        _unitOfWork.Tags.GetByNameAsync(ownerUserId, "test")
            .Returns((Tag?)null);
        
        _unitOfWork.Tags.AddAsync(Arg.Any<Tag>())
            .Returns(mockTag);
        
        _unitOfWork.RawCaptures.AddAsync(Arg.Any<RawCapture>())
            .Returns(callInfo => callInfo.Arg<RawCapture>());
        
        _unitOfWork.SaveChangesAsync()
            .Returns(1);
        
        var result = await _service.CreateCaptureAsync(ownerUserId, request);
        
        result.Should().NotBeNull();
        result.SourceUrl.Should().Be(request.SourceUrl);
        result.ContentType.Should().Be(request.ContentType);
        result.Status.Should().Be(CaptureStatus.Pending);
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
}
