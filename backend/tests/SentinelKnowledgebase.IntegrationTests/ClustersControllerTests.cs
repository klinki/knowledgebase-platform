using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using SentinelKnowledgebase.Application.DTOs.Clusters;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using Xunit;

namespace SentinelKnowledgebase.IntegrationTests;

[Collection("IntegrationTests")]
public class ClustersControllerTests
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IntegrationTestFixture _fixture;

    public ClustersControllerTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetClusters_ShouldReturn401_WhenAnonymous()
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync("/api/v1/clusters");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetClusters_ShouldReturnOwnerScopedClusters_WhenAuthenticated()
    {
        using var ownerClient = await _fixture.CreateAuthenticatedClientAsync();
        var member = await _fixture.CreateMemberClientAsync();
        using var memberClient = member.Client;

        var ownerUserId = await _fixture.GetUserIdByEmailAsync(IntegrationTestFixture.BootstrapAdminEmail);
        var memberUserId = await _fixture.GetUserIdByEmailAsync(member.Email);

        var ownerClusterId = await SeedClusterAsync(ownerUserId, "Owner Topic");
        _ = await SeedClusterAsync(memberUserId, "Member Topic");

        var response = await ownerClient.GetAsync("/api/v1/clusters");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var clusters = await response.Content.ReadFromJsonAsync<List<TopicClusterSummaryDto>>(ResponseJsonOptions);
        clusters.Should().NotBeNull();
        clusters!.Should().ContainSingle(cluster => cluster.Id == ownerClusterId);
        clusters.Should().NotContain(cluster => cluster.Title == "Member Topic");

        var memberResponse = await memberClient.GetAsync("/api/v1/clusters");
        memberResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var memberClusters = await memberResponse.Content.ReadFromJsonAsync<List<TopicClusterSummaryDto>>(ResponseJsonOptions);
        memberClusters.Should().NotBeNull();
        memberClusters!.Should().ContainSingle(cluster => cluster.Title == "Member Topic");
    }

    [Fact]
    public async Task GetCluster_ShouldReturnOrderedMembers_AndRejectForeignOwner()
    {
        using var ownerClient = await _fixture.CreateAuthenticatedClientAsync();
        var member = await _fixture.CreateMemberClientAsync();
        using var memberClient = member.Client;

        var ownerUserId = await _fixture.GetUserIdByEmailAsync(IntegrationTestFixture.BootstrapAdminEmail);
        var clusterId = await SeedClusterAsync(ownerUserId, "Ordered Topic");

        var response = await ownerClient.GetAsync($"/api/v1/clusters/{clusterId}");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var cluster = await response.Content.ReadFromJsonAsync<TopicClusterDetailDto>(ResponseJsonOptions);
        cluster.Should().NotBeNull();
        cluster!.SuggestedLabel.Category.Should().Be("Topic");
        cluster.Members.Select(memberDto => memberDto.Rank).Should().ContainInOrder(1, 2, 3);
        cluster.Members[0].SimilarityToCentroid.Should().BeGreaterThan(cluster.Members[2].SimilarityToCentroid);

        var foreignResponse = await memberClient.GetAsync($"/api/v1/clusters/{clusterId}");
        foreignResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetClusterList_ShouldReturnPagedClusters_WhenAuthenticated()
    {
        var member = await _fixture.CreateMemberClientAsync();
        using var client = member.Client;
        var ownerUserId = await _fixture.GetUserIdByEmailAsync(member.Email);

        await SeedClusterAsync(ownerUserId, "Large Topic", memberCount: 5);
        await SeedClusterAsync(ownerUserId, "Medium Topic", memberCount: 4);
        await SeedClusterAsync(ownerUserId, "Small Topic", memberCount: 3);

        var response = await client.GetAsync("/api/v1/clusters/list?page=2&pageSize=1");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<TopicClusterListPageDto>(ResponseJsonOptions);
        page.Should().NotBeNull();
        page!.Page.Should().Be(2);
        page.PageSize.Should().Be(1);
        page.TotalCount.Should().Be(3);
        page.Items.Should().ContainSingle();
        page.Items[0].Title.Should().Be("Medium Topic");
    }

    [Fact]
    public async Task GetClusterList_ShouldSearchByTitle_WhenQueryMatchesTitle()
    {
        var member = await _fixture.CreateMemberClientAsync();
        using var client = member.Client;
        var ownerUserId = await _fixture.GetUserIdByEmailAsync(member.Email);
        var titleToken = $"signal-{Guid.NewGuid():N}";

        await SeedClusterAsync(ownerUserId, $"Signal Routing {titleToken}");
        await SeedClusterAsync(ownerUserId, "Storage Ops");

        var response = await client.GetAsync($"/api/v1/clusters/list?query={titleToken}");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<TopicClusterListPageDto>(ResponseJsonOptions);
        page.Should().NotBeNull();
        page!.Items.Should().Contain(cluster => cluster.Title == $"Signal Routing {titleToken}");
        page.Items.Should().NotContain(cluster => cluster.Title == "Storage Ops");
    }

    [Fact]
    public async Task GetClusterList_ShouldSearchByDescription_WhenQueryMatchesDescription()
    {
        var member = await _fixture.CreateMemberClientAsync();
        using var client = member.Client;
        var ownerUserId = await _fixture.GetUserIdByEmailAsync(member.Email);
        var descriptionToken = $"edge-{Guid.NewGuid():N}";

        await SeedClusterAsync(ownerUserId, "Compute", description: $"Latency control plane for {descriptionToken}");
        await SeedClusterAsync(ownerUserId, "Storage", description: "Backups and cold storage workflows");

        var response = await client.GetAsync($"/api/v1/clusters/list?query={descriptionToken}");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<TopicClusterListPageDto>(ResponseJsonOptions);
        page.Should().NotBeNull();
        page!.Items.Should().Contain(cluster => cluster.Title == "Compute");
        page.Items.Should().NotContain(cluster => cluster.Title == "Storage");
    }

    [Fact]
    public async Task GetClusterList_ShouldSearchByKeywords_AndRemainOwnerScoped()
    {
        var member = await _fixture.CreateMemberClientAsync();
        using var client = member.Client;
        var ownerUserId = await _fixture.GetUserIdByEmailAsync(member.Email);

        var otherMember = await _fixture.CreateMemberClientAsync();
        var otherUserId = await _fixture.GetUserIdByEmailAsync(otherMember.Email);

        await SeedClusterAsync(ownerUserId, "Pipeline", keywords: ["orchestration", "workflows", "timers"]);
        await SeedClusterAsync(otherUserId, "Foreign Pipeline", keywords: ["orchestration", "private", "other"]);

        var response = await client.GetAsync("/api/v1/clusters/list?query=orchestration");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<TopicClusterListPageDto>(ResponseJsonOptions);
        page.Should().NotBeNull();
        page!.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle();
        page.Items[0].Title.Should().Be("Pipeline");
    }

    [Fact]
    public async Task GetClusterList_ShouldSortByMemberCountAscending_WhenRequested()
    {
        var member = await _fixture.CreateMemberClientAsync();
        using var client = member.Client;
        var ownerUserId = await _fixture.GetUserIdByEmailAsync(member.Email);

        await SeedClusterAsync(ownerUserId, "Large Topic", memberCount: 5);
        await SeedClusterAsync(ownerUserId, "Medium Topic", memberCount: 4);
        await SeedClusterAsync(ownerUserId, "Small Topic", memberCount: 3);

        var response = await client.GetAsync("/api/v1/clusters/list?sortField=memberCount&sortDirection=asc&pageSize=10");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<TopicClusterListPageDto>(ResponseJsonOptions);
        page.Should().NotBeNull();
        page!.Items.Select(item => item.Title).Should().ContainInOrder("Small Topic", "Medium Topic", "Large Topic");
    }

    [Fact]
    public async Task GetClusterList_ShouldSortByUpdatedAt_WhenRequested()
    {
        var member = await _fixture.CreateMemberClientAsync();
        using var client = member.Client;
        var ownerUserId = await _fixture.GetUserIdByEmailAsync(member.Email);
        var now = DateTime.UtcNow;

        await SeedClusterAsync(ownerUserId, "Older Topic", updatedAt: now.AddHours(-3));
        await SeedClusterAsync(ownerUserId, "Newest Topic", updatedAt: now.AddHours(-1));
        await SeedClusterAsync(ownerUserId, "Middle Topic", updatedAt: now.AddHours(-2));

        var response = await client.GetAsync("/api/v1/clusters/list?sortField=updatedAt&sortDirection=desc&pageSize=10");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<TopicClusterListPageDto>(ResponseJsonOptions);
        page.Should().NotBeNull();
        page!.Items.Select(item => item.Title).Should().ContainInOrder("Newest Topic", "Middle Topic", "Older Topic");
    }

    [Fact]
    public async Task GetClusterList_ShouldSortByTitle_WhenRequested()
    {
        var member = await _fixture.CreateMemberClientAsync();
        using var client = member.Client;
        var ownerUserId = await _fixture.GetUserIdByEmailAsync(member.Email);

        await SeedClusterAsync(ownerUserId, "Zulu Topic");
        await SeedClusterAsync(ownerUserId, "Alpha Topic");
        await SeedClusterAsync(ownerUserId, "Bravo Topic");

        var response = await client.GetAsync("/api/v1/clusters/list?sortField=title&sortDirection=asc&pageSize=10");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<TopicClusterListPageDto>(ResponseJsonOptions);
        page.Should().NotBeNull();
        page!.Items.Select(item => item.Title).Should().ContainInOrder("Alpha Topic", "Bravo Topic", "Zulu Topic");
    }

    private async Task<Guid> SeedClusterAsync(
        Guid ownerUserId,
        string title,
        int memberCount = 3,
        string? description = null,
        IReadOnlyList<string>? keywords = null,
        DateTime? updatedAt = null)
    {
        var clusterId = Guid.NewGuid();

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            var now = updatedAt ?? DateTime.UtcNow;
            var insightIds = new List<Guid>();

            for (var index = 0; index < memberCount; index++)
            {
                var captureId = Guid.NewGuid();
                var insightId = Guid.NewGuid();
                insightIds.Add(insightId);

                dbContext.RawCaptures.Add(new RawCapture
                {
                    Id = captureId,
                    OwnerUserId = ownerUserId,
                    SourceUrl = $"https://example.com/clusters/{title.ToLowerInvariant().Replace(' ', '-')}/{index}",
                    ContentType = ContentType.Article,
                    RawContent = $"Cluster capture {index}",
                    Status = CaptureStatus.Completed,
                    CreatedAt = now.AddMinutes(index),
                    ProcessedAt = now.AddMinutes(index + 1)
                });

                dbContext.ProcessedInsights.Add(new ProcessedInsight
                {
                    Id = insightId,
                    OwnerUserId = ownerUserId,
                    RawCaptureId = captureId,
                    Title = $"{title} insight {index}",
                    Summary = $"{title} summary {index}",
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
                Description = description ?? $"{title} description",
                KeywordsJson = JsonSerializer.Serialize(keywords ?? new[] { "cluster", "topic", "semantic" }),
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
