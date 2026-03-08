using AwesomeAssertions;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Domain.Enums;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace SentinelKnowledgebase.IntegrationTests;

[Collection("IntegrationTests")]
public class CaptureControllerTests
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IntegrationTestFixture _fixture;
    
    public CaptureControllerTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task CreateCapture_ShouldReturn401_WhenAnonymous()
    {
        using var client = _fixture.CreateClient();

        var request = new CaptureRequestDto
        {
            SourceUrl = "https://example.com/article",
            ContentType = ContentType.Article,
            RawContent = "This is a test article content with valuable information.",
            Metadata = JsonSerializer.Serialize(new { author = "Test Author" })
        };

        var response = await client.PostAsJsonAsync("/api/v1/capture", request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCapture_ShouldReturn202Accepted_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var request = new CaptureRequestDto
        {
            SourceUrl = "https://example.com/article",
            ContentType = ContentType.Article,
            RawContent = "This is a test article content with valuable information.",
            Metadata = JsonSerializer.Serialize(new { author = "Test Author" }),
            Tags = new List<string> { "test", "integration" }
        };
        
        var response = await client.PostAsJsonAsync("/api/v1/capture", request);
        
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
        
        var content = await response.Content.ReadFromJsonAsync<CaptureAcceptedDto>();
        content.Should().NotBeNull();
        content!.Id.Should().NotBe(Guid.Empty);
    }
    
    [Fact]
    public async Task GetCapture_ShouldReturn401_WhenAnonymous()
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync($"/api/v1/capture/{Guid.NewGuid()}");
        
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCapture_ShouldReturn404ForNonexistent_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/v1/capture/{Guid.NewGuid()}");
        
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task GetAllCaptures_ShouldReturnEmptyList_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v1/capture");
        
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<CaptureResponseDto>>(ResponseJsonOptions);
        content.Should().NotBeNull();
    }
    
    [Fact]
    public async Task CreateCapture_WithInvalidUrl_ShouldReturn400_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var request = new CaptureRequestDto
        {
            SourceUrl = "not-a-valid-url",
            ContentType = ContentType.Article,
            RawContent = "Test content"
        };
        
        var response = await client.PostAsJsonAsync("/api/v1/capture", request);
        
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CaptureAccess_ShouldReturn404_ForForeignOwner()
    {
        using var ownerClient = await _fixture.CreateAuthenticatedClientAsync();
        var member = await _fixture.CreateMemberClientAsync();
        using var foreignClient = member.Client;

        var createResponse = await ownerClient.PostAsJsonAsync("/api/v1/capture", new CaptureRequestDto
        {
            SourceUrl = $"https://example.com/{Guid.NewGuid():N}",
            ContentType = ContentType.Article,
            RawContent = "Owner scoped capture content."
        });

        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
        var accepted = await createResponse.Content.ReadFromJsonAsync<CaptureAcceptedDto>();
        accepted.Should().NotBeNull();

        var foreignGetResponse = await foreignClient.GetAsync($"/api/v1/capture/{accepted!.Id}");
        foreignGetResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);

        var foreignDeleteResponse = await foreignClient.DeleteAsync($"/api/v1/capture/{accepted.Id}");
        foreignDeleteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);

        var ownerGetResponse = await ownerClient.GetAsync($"/api/v1/capture/{accepted.Id}");
        ownerGetResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}
