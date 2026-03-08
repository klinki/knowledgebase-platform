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

    [Fact]
    public async Task GetOverview_ShouldExcludeOtherUsersData_WhenAuthenticated()
    {
        using var adminClient = await _fixture.CreateAuthenticatedClientAsync();
        var member = await _fixture.CreateMemberClientAsync();
        using var memberClient = member.Client;

        var adminUrl = $"https://example.com/admin/{Guid.NewGuid():N}";
        var adminTag = $"admin-tag-{Guid.NewGuid():N}";
        var memberUrl = $"https://example.com/member/{Guid.NewGuid():N}";
        var memberTag = $"member-tag-{Guid.NewGuid():N}";

        var adminCreateResponse = await adminClient.PostAsJsonAsync("/api/v1/capture", new CaptureRequestDto
        {
            SourceUrl = adminUrl,
            ContentType = ContentType.Article,
            RawContent = "Admin-owned dashboard capture.",
            Metadata = JsonSerializer.Serialize(new { source = "admin" }),
            Tags = new List<string> { adminTag }
        });
        adminCreateResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);

        var memberCreateResponse = await memberClient.PostAsJsonAsync("/api/v1/capture", new CaptureRequestDto
        {
            SourceUrl = memberUrl,
            ContentType = ContentType.Article,
            RawContent = "Member-owned dashboard capture.",
            Metadata = JsonSerializer.Serialize(new { source = "member" }),
            Tags = new List<string> { memberTag }
        });
        memberCreateResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);

        var adminOverviewResponse = await adminClient.GetAsync("/api/v1/dashboard/overview");
        adminOverviewResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var adminOverview = await adminOverviewResponse.Content.ReadFromJsonAsync<DashboardOverviewDto>(ResponseJsonOptions);
        adminOverview.Should().NotBeNull();
        adminOverview!.RecentCaptures.Should().Contain(capture => capture.SourceUrl == adminUrl);
        adminOverview.RecentCaptures.Should().NotContain(capture => capture.SourceUrl == memberUrl);
        adminOverview.TopTags.Should().Contain(tag => tag.Name == adminTag);
        adminOverview.TopTags.Should().NotContain(tag => tag.Name == memberTag);

        var memberOverviewResponse = await memberClient.GetAsync("/api/v1/dashboard/overview");
        memberOverviewResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var memberOverview = await memberOverviewResponse.Content.ReadFromJsonAsync<DashboardOverviewDto>(ResponseJsonOptions);
        memberOverview.Should().NotBeNull();
        memberOverview!.RecentCaptures.Should().Contain(capture => capture.SourceUrl == memberUrl);
        memberOverview.RecentCaptures.Should().NotContain(capture => capture.SourceUrl == adminUrl);
        memberOverview.TopTags.Should().Contain(tag => tag.Name == memberTag);
        memberOverview.TopTags.Should().NotContain(tag => tag.Name == adminTag);
    }
}
