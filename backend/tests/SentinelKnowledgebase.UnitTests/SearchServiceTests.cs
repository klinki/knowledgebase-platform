using FluentAssertions;
using NSubstitute;
using Pgvector;
using SentinelKnowledgebase.Application.DTOs.Search;
using SentinelKnowledgebase.Application.Services;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Repositories;
using Xunit;

namespace SentinelKnowledgebase.UnitTests;

public class SearchServiceTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContentProcessor _contentProcessor;
    private readonly SearchService _service;
    
    public SearchServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _contentProcessor = Substitute.For<IContentProcessor>();
        _service = new SearchService(_unitOfWork, _contentProcessor);
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
        _contentProcessor.GenerateEmbeddingAsync(request.Query)
            .Returns(queryEmbedding);
        
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
        
        _unitOfWork.ProcessedInsights.GetAllAsync()
            .Returns(insights);
        
        _unitOfWork.EmbeddingVectors.GetByProcessedInsightIdAsync(Arg.Any<Guid>())
            .Returns(new EmbeddingVector { Vector = new Vector(queryEmbedding) });
        
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
        _contentProcessor.GenerateEmbeddingAsync(request.Query)
            .Returns(queryEmbedding);
        
        var insights = new List<ProcessedInsight>();
        _unitOfWork.ProcessedInsights.GetAllAsync()
            .Returns(insights);
        
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
        
        _unitOfWork.ProcessedInsights.GetAllAsync()
            .Returns(insights);
        
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
        
        _unitOfWork.ProcessedInsights.GetAllAsync()
            .Returns(insights);
        
        var result = await _service.SearchByTagsAsync(request);
        
        result.Should().BeEmpty();
    }
}
