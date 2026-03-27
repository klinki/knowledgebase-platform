using AwesomeAssertions;
using NSubstitute;
using SentinelKnowledgebase.Application.DTOs.Labels;
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
        var ownerUserId = Guid.NewGuid();
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
                Tags = new List<string>(),
                Labels = new List<LabelRecord>
                {
                    new() { Category = "Language", Value = "English" }
                }
            }
        };
        
        _unitOfWork.ProcessedInsights
            .SemanticSearchAsync(ownerUserId, queryEmbedding, request.TopK, request.Threshold)
            .Returns(searchResults);
        
        var result = await _service.SemanticSearchAsync(ownerUserId, request);
        
        result.Should().NotBeEmpty();
        result.First().Labels.Should().ContainSingle(label => label.Category == "Language" && label.Value == "English");
    }
    
    [Fact]
    public async Task SemanticSearchAsync_ShouldReturnEmptyList_WhenNoEmbeddings()
    {
        var ownerUserId = Guid.NewGuid();
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
            .SemanticSearchAsync(ownerUserId, queryEmbedding, request.TopK, request.Threshold)
            .Returns(Enumerable.Empty<SemanticSearchRecord>());
        
        var result = await _service.SemanticSearchAsync(ownerUserId, request);
        
        result.Should().BeEmpty();
    }
    
    [Fact]
    public async Task SearchByTagsAsync_ShouldReturnMatchingInsights()
    {
        var ownerUserId = Guid.NewGuid();
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
                Labels = new List<LabelRecord>
                {
                    new() { Category = "Source", Value = "Twitter" }
                },
                ProcessedAt = DateTime.UtcNow,
                SourceUrl = "https://example.com"
            }
        };
        
        _unitOfWork.ProcessedInsights.SearchByTagsAsync(ownerUserId, request.Tags, request.MatchAll)
            .Returns(insights);
        
        var result = await _service.SearchByTagsAsync(ownerUserId, request);
        
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Test");
        result.First().Labels.Should().ContainSingle(label => label.Category == "Source" && label.Value == "Twitter");
    }
    
    [Fact]
    public async Task SearchByTagsAsync_WithMatchAll_ShouldRequireAllTags()
    {
        var ownerUserId = Guid.NewGuid();
        var request = new TagSearchRequestDto
        {
            Tags = new List<string> { "test", "important" },
            MatchAll = true
        };
        
        _unitOfWork.ProcessedInsights.SearchByTagsAsync(ownerUserId, request.Tags, request.MatchAll)
            .Returns(Enumerable.Empty<TagSearchRecord>());
        
        var result = await _service.SearchByTagsAsync(ownerUserId, request);
        
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchByLabelsAsync_ShouldReturnMatchingInsights()
    {
        var ownerUserId = Guid.NewGuid();
        var request = new LabelSearchRequestDto
        {
            Labels =
            [
                new LabelAssignmentDto { Category = "Language", Value = "English" }
            ],
            MatchAll = false
        };

        var insights = new List<LabelSearchRecord>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Labeled Insight",
                Summary = "Summary",
                Tags = ["test"],
                Labels =
                [
                    new LabelRecord { Category = "Language", Value = "English" }
                ],
                ProcessedAt = DateTime.UtcNow,
                SourceUrl = "https://example.com"
            }
        };

        _unitOfWork.ProcessedInsights.SearchByLabelsAsync(
                ownerUserId,
                Arg.Any<IReadOnlyCollection<LabelRecord>>(),
                request.MatchAll)
            .Returns(insights);

        var result = await _service.SearchByLabelsAsync(ownerUserId, request);

        result.Should().ContainSingle();
        result.First().Labels.Should().ContainSingle(label => label.Category == "Language" && label.Value == "English");
    }
}
