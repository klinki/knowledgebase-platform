using FluentAssertions;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Domain.Enums;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SentinelKnowledgebase.IntegrationTests;

[Collection("IntegrationTests")]
public class CaptureControllerTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _httpClient;
    
    public CaptureControllerTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _httpClient = fixture.HttpClient;
    }
    
    [Fact]
    public async Task CreateCapture_ShouldReturn202Accepted()
    {
        var request = new CaptureRequestDto
        {
            SourceUrl = "https://example.com/article",
            ContentType = ContentType.Article,
            RawContent = "This is a test article content with valuable information.",
            Metadata = JsonSerializer.Serialize(new { author = "Test Author" }),
            Tags = new List<string> { "test", "integration" }
        };
        
        var response = await _httpClient.PostAsJsonAsync("/api/v1/capture", request);
        
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
        
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("id").GetGuid().Should().NotBe(Guid.Empty);
    }
    
    [Fact]
    public async Task GetCapture_ShouldReturn404ForNonexistent()
    {
        var response = await _httpClient.GetAsync($"/api/v1/capture/{Guid.NewGuid()}");
        
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task GetAllCaptures_ShouldReturnEmptyList()
    {
        var response = await _httpClient.GetAsync("/api/v1/capture");
        
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<CaptureResponseDto>>();
        content.Should().NotBeNull();
    }
    
    [Fact]
    public async Task CreateCapture_WithInvalidUrl_ShouldReturn400()
    {
        var request = new CaptureRequestDto
        {
            SourceUrl = "not-a-valid-url",
            ContentType = ContentType.Article,
            RawContent = "Test content"
        };
        
        var response = await _httpClient.PostAsJsonAsync("/api/v1/capture", request);
        
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
}
