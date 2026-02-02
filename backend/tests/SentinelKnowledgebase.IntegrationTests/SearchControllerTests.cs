using FluentAssertions;
using SentinelKnowledgebase.Application.DTOs.Search;
using System.Net.Http.Json;
using Xunit;

namespace SentinelKnowledgebase.IntegrationTests;

[Collection("IntegrationTests")]
public class SearchControllerTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _httpClient;
    
    public SearchControllerTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _httpClient = fixture.HttpClient;
    }
    
    [Fact]
    public async Task SemanticSearch_ShouldReturnEmptyList()
    {
        var request = new SemanticSearchRequestDto
        {
            Query = "test query",
            TopK = 5,
            Threshold = 0.5
        };
        
        var response = await _httpClient.PostAsJsonAsync("/api/v1/search/semantic", request);
        
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<SemanticSearchResultDto>>();
        content.Should().NotBeNull();
        content.Should().BeEmpty();
    }
    
    [Fact]
    public async Task TagSearch_ShouldReturnEmptyList()
    {
        var request = new TagSearchRequestDto
        {
            Tags = new List<string> { "nonexistent-tag" },
            MatchAll = false
        };
        
        var response = await _httpClient.PostAsJsonAsync("/api/v1/search/tags", request);
        
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<TagSearchResultDto>>();
        content.Should().NotBeNull();
        content.Should().BeEmpty();
    }
    
    [Fact]
    public async Task SemanticSearch_WithEmptyQuery_ShouldReturn400()
    {
        var request = new SemanticSearchRequestDto
        {
            Query = "",
            TopK = 5
        };
        
        var response = await _httpClient.PostAsJsonAsync("/api/v1/search/semantic", request);
        
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task TagSearch_WithEmptyTags_ShouldReturn400()
    {
        var request = new TagSearchRequestDto
        {
            Tags = new List<string>()
        };
        
        var response = await _httpClient.PostAsJsonAsync("/api/v1/search/tags", request);
        
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
}
