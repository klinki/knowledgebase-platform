using System.Net;
using System.Net.Http.Json;
using SentinelKnowledgebase.Application.DTOs;
using SentinelKnowledgebase.Domain.Enums;

namespace SentinelKnowledgebase.Tests.Integration;

[Collection("IntegrationTests")]
public class SearchControllerTests
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _client;

    public SearchControllerTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task SemanticSearch_WithValidRequest_ReturnsResults()
    {
        // Arrange
        var request = new SemanticSearchRequest
        {
            Query = "test query",
            Limit = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/search/semantic", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<IEnumerable<SemanticSearchResponse>>();
        Assert.NotNull(results);
    }

    [Fact]
    public async Task SemanticSearch_WithInvalidLimit_ReturnsBadRequest()
    {
        // Arrange
        var request = new SemanticSearchRequest
        {
            Query = "test",
            Limit = 0 // Invalid - must be >= 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/search/semantic", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchByTag_WithValidTag_ReturnsResults()
    {
        // Arrange - First create a capture with tags
        var captureRequest = new CaptureRequest
        {
            SourceUrl = "https://example.com/test",
            RawContent = "Test content with tags",
            Source = CaptureSource.WebPage,
            Tags = new List<string> { "important", "reference" }
        };

        await _client.PostAsJsonAsync("/api/v1/capture", captureRequest);

        var searchRequest = new TagSearchRequest
        {
            Tag = "important"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/search/tags", searchRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<IEnumerable<TagSearchResponse>>();
        Assert.NotNull(results);
    }

    [Fact]
    public async Task SearchByTag_WithEmptyTag_ReturnsBadRequest()
    {
        // Arrange
        var request = new TagSearchRequest
        {
            Tag = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/search/tags", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
