using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using AwesomeAssertions;

using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Application.DTOs.Dashboard;
using SentinelKnowledgebase.Domain.Entities;
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

    [Fact]
    public async Task GetOverview_ShouldIncludeTopicClusters_WhenAvailable()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var ownerUserId = await _fixture.GetUserIdByEmailAsync(IntegrationTestFixture.BootstrapAdminEmail);
        var clusterId = await SeedClusterAsync(ownerUserId, "AI Infrastructure", "Cluster for infrastructure notes.");

        var overviewResponse = await client.GetAsync("/api/v1/dashboard/overview");

        overviewResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var overview = await overviewResponse.Content.ReadFromJsonAsync<DashboardOverviewDto>(ResponseJsonOptions);
        overview.Should().NotBeNull();
        overview!.TopicClusters.Should().ContainSingle(cluster => cluster.Id == clusterId);
        overview.TopicClusters[0].SuggestedLabel.Category.Should().Be("Topic");
        overview.TopicClusters[0].RepresentativeInsights.Should().NotBeEmpty();
    }

    private async Task<Guid> SeedClusterAsync(Guid ownerUserId, string title, string description)
    {
        var clusterId = Guid.NewGuid();
        var insightIds = new List<Guid>();

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            var now = DateTime.UtcNow;

            for (var index = 0; index < 3; index++)
            {
                var captureId = Guid.NewGuid();
                var insightId = Guid.NewGuid();
                insightIds.Add(insightId);

                dbContext.RawCaptures.Add(new RawCapture
                {
                    Id = captureId,
                    OwnerUserId = ownerUserId,
                    SourceUrl = $"https://example.com/topic/{index}",
                    ContentType = ContentType.Article,
                    RawContent = $"Topic capture {index}",
                    Status = CaptureStatus.Completed,
                    CreatedAt = now.AddMinutes(index),
                    ProcessedAt = now.AddMinutes(index + 1)
                });

                dbContext.ProcessedInsights.Add(new ProcessedInsight
                {
                    Id = insightId,
                    OwnerUserId = ownerUserId,
                    RawCaptureId = captureId,
                    Title = $"Topic insight {index}",
                    Summary = $"Topic summary {index}",
                    KeyInsights = JsonSerializer.Serialize(new[] { $"Insight {index}" }),
                    ActionItems = JsonSerializer.Serialize(new[] { $"Action {index}" }),
                    ProcessedAt = now.AddMinutes(index + 1)
                });
            }

            dbContext.InsightClusters.Add(new InsightCluster
            {
                Id = clusterId,
                OwnerUserId = ownerUserId,
                Title = title,
                Description = description,
                KeywordsJson = JsonSerializer.Serialize(new[] { "ai", "infra", "ops" }),
                MemberCount = insightIds.Count,
                RepresentativeProcessedInsightId = insightIds[0],
                LastComputedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });

            dbContext.InsightClusterMemberships.AddRange(insightIds.Select((insightId, index) => new InsightClusterMembership
            {
                InsightClusterId = clusterId,
                ProcessedInsightId = insightId,
                Rank = index + 1,
                SimilarityToCentroid = 0.99 - (index * 0.01),
                CreatedAt = now
            }));

            await Task.CompletedTask;
        });

        return clusterId;
    }
}
