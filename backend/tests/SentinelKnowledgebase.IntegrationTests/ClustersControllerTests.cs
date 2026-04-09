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

    private async Task<Guid> SeedClusterAsync(Guid ownerUserId, string title)
    {
        var clusterId = Guid.NewGuid();

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            var now = DateTime.UtcNow;
            var insightIds = new List<Guid>();

            for (var index = 0; index < 3; index++)
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
                Description = $"{title} description",
                KeywordsJson = JsonSerializer.Serialize(new[] { "cluster", "topic", "semantic" }),
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
