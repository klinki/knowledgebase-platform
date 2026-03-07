using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using AwesomeAssertions;

using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Application.DTOs.Dashboard;
using SentinelKnowledgebase.Domain.Enums;

using Xunit;

namespace SentinelKnowledgebase.IntegrationTests;

[Collection("IntegrationTests")]
public class DashboardControllerTests
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IntegrationTestFixture _fixture;

    public DashboardControllerTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetOverview_ShouldReturn401_WhenAnonymous()
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync("/api/v1/dashboard/overview");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOverview_ShouldReturnRecentCapturesTagsAndStats_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var uniqueTag = $"dashboard-{Guid.NewGuid():N}";
        var uniqueUrl = $"https://example.com/{Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("/api/v1/capture", new CaptureRequestDto
        {
            SourceUrl = uniqueUrl,
            ContentType = ContentType.Article,
            RawContent = "Dashboard overview test capture content.",
            Metadata = JsonSerializer.Serialize(new { source = "integration-test" }),
            Tags = new List<string> { uniqueTag }
        });

        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);

        var overviewResponse = await client.GetAsync("/api/v1/dashboard/overview");

        overviewResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var overview = await overviewResponse.Content.ReadFromJsonAsync<DashboardOverviewDto>(ResponseJsonOptions);
        overview.Should().NotBeNull();
        overview!.Stats.TotalCaptures.Should().BeGreaterThan(0);
        overview.Stats.ActiveTags.Should().BeGreaterThan(0);
        overview.RecentCaptures.Should().Contain(capture => capture.SourceUrl == uniqueUrl);
        overview.TopTags.Should().Contain(tag => tag.Name == uniqueTag);
    }
}
