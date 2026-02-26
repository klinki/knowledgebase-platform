using FluentAssertions;
using NSubstitute;
using SentinelKnowledgebase.Application.DTOs.Search;
using SentinelKnowledgebase.Application.Services;
using SentinelKnowledgebase.Application.Services.Interfaces;
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
        
        var searchResults = new List<SemanticSearchRecord>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Test Insight",
                Summary = "Test summary",
                Similarity = 0.95,
                SourceUrl = "https://example.com",
                Tags = new List<string>()
            }
        };
        
        _unitOfWork.ProcessedInsights
            .SemanticSearchAsync(queryEmbedding, request.TopK, request.Threshold)
            .Returns(searchResults);
        
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
        
        _unitOfWork.ProcessedInsights
            .SemanticSearchAsync(queryEmbedding, request.TopK, request.Threshold)
            .Returns(Enumerable.Empty<SemanticSearchRecord>());
        
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
        
        var insights = new List<TagSearchRecord>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Test",
                Summary = "Summary",
                Tags = new List<string> { "test" },
                ProcessedAt = DateTime.UtcNow,
                SourceUrl = "https://example.com"
            }
        };
        
        _unitOfWork.ProcessedInsights.SearchByTagsAsync(request.Tags, request.MatchAll)
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
        
        _unitOfWork.ProcessedInsights.SearchByTagsAsync(request.Tags, request.MatchAll)
            .Returns(Enumerable.Empty<TagSearchRecord>());
        
        var result = await _service.SearchByTagsAsync(request);
        
        result.Should().BeEmpty();
    }
}
