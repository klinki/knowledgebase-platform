using System.Net.Http.Json;
using System.Text.Json;

using AwesomeAssertions;

using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Application.DTOs.Dashboard;
using SentinelKnowledgebase.Domain.Enums;

using Xunit;

namespace SentinelKnowledgebase.IntegrationTests;

[Collection("IntegrationTests")]
public class TagsControllerTests
{
    private readonly IntegrationTestFixture _fixture;

    public TagsControllerTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetTags_ShouldReturn401_WhenAnonymous()
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync("/api/v1/tags");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTags_ShouldReturnTagSummaries_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var uniqueTag = $"tag-summary-{Guid.NewGuid():N}";

        foreach (var index in Enumerable.Range(1, 2))
        {
            var response = await client.PostAsJsonAsync("/api/v1/capture", new CaptureRequestDto
            {
                SourceUrl = $"https://example.com/tags/{Guid.NewGuid():N}/{index}",
                ContentType = ContentType.Article,
                RawContent = $"Tag summary capture {index}.",
                Metadata = JsonSerializer.Serialize(new { source = "integration-test" }),
                Tags = new List<string> { uniqueTag }
            });

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
        }

        var tagsResponse = await client.GetAsync("/api/v1/tags");

        tagsResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var tags = await tagsResponse.Content.ReadFromJsonAsync<List<TagSummaryDto>>();
        tags.Should().NotBeNull();
        tags!.Should().Contain(tag => tag.Name == uniqueTag && tag.Count == 2);
    }
}
