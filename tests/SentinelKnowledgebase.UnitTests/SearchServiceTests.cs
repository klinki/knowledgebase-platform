using FluentAssertions;
using Moq;
using SentinelKnowledgebase.Application.DTOs.Search;
using SentinelKnowledgebase.Application.Services;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Repositories;
using Xunit;

namespace SentinelKnowledgebase.UnitTests;

public class SearchServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IContentProcessor> _mockContentProcessor;
    private readonly SearchService _service;
    
    public SearchServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockContentProcessor = new Mock<IContentProcessor>();
        _service = new SearchService(_mockUnitOfWork.Object, _mockContentProcessor.Object);
    }
    
    [Fact]
    public async Task SemanticSearchAsync_ShouldReturnResults()
    {
        var request = new SemanticSearchRequestDto
        {
            Query = "test query",
            TopK = 5,
            Threshold = 0.3
        };
        
        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        _mockContentProcessor.Setup(x => x.GenerateEmbeddingAsync(request.Query))
            .ReturnsAsync(queryEmbedding);
        
        var insights = new List<ProcessedInsight>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Test Insight",
                Summary = "Test summary",
                Tags = new List<Tag>(),
                RawCapture = new RawCapture { SourceUrl = "https://example.com" }
            }
        };
        
        _mockUnitOfWork.Setup(x => x.ProcessedInsights.GetAllAsync())
            .ReturnsAsync(insights);
        
        _mockUnitOfWork.Setup(x => x.EmbeddingVectors.GetByProcessedInsightIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new EmbeddingVector { Vector = queryEmbedding });
        
        var result = await _service.SemanticSearchAsync(request);
        
        result.Should().NotBeEmpty();
    }
    
    [Fact]
    public async Task SemanticSearchAsync_ShouldReturnEmptyList_WhenNoEmbeddings()
    {
        var request = new SemanticSearchRequestDto
        {
            Query = "test query",
            TopK = 5,
            Threshold = 0.5
        };
        
        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        _mockContentProcessor.Setup(x => x.GenerateEmbeddingAsync(request.Query))
            .ReturnsAsync(queryEmbedding);
        
        var insights = new List<ProcessedInsight>();
        _mockUnitOfWork.Setup(x => x.ProcessedInsights.GetAllAsync())
            .ReturnsAsync(insights);
        
        var result = await _service.SemanticSearchAsync(request);
        
        result.Should().BeEmpty();
    }
    
    [Fact]
    public async Task SearchByTagsAsync_ShouldReturnMatchingInsights()
    {
        var request = new TagSearchRequestDto
        {
            Tags = new List<string> { "test" },
            MatchAll = false
        };
        
        var insights = new List<ProcessedInsight>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Test",
                Summary = "Summary",
                Tags = new List<Tag> { new Tag { Name = "test" } },
                ProcessedAt = DateTime.UtcNow,
                RawCapture = new RawCapture { SourceUrl = "https://example.com" }
            }
        };
        
        _mockUnitOfWork.Setup(x => x.ProcessedInsights.GetAllAsync())
            .ReturnsAsync(insights);
        
        var result = await _service.SearchByTagsAsync(request);
        
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Test");
    }
    
    [Fact]
    public async Task SearchByTagsAsync_WithMatchAll_ShouldRequireAllTags()
    {
        var request = new TagSearchRequestDto
        {
            Tags = new List<string> { "test", "important" },
            MatchAll = true
        };
        
        var insights = new List<ProcessedInsight>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Test",
                Summary = "Summary",
                Tags = new List<Tag> { new Tag { Name = "test" } },
                ProcessedAt = DateTime.UtcNow,
                RawCapture = new RawCapture { SourceUrl = "https://example.com" }
            }
        };
        
        _mockUnitOfWork.Setup(x => x.ProcessedInsights.GetAllAsync())
            .ReturnsAsync(insights);
        
        var result = await _service.SearchByTagsAsync(request);
        
        result.Should().BeEmpty();
    }
}
