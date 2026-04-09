using AwesomeAssertions;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Application.DTOs.Labels;
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
    public async Task CreateCapturesBulk_ShouldReturn202Accepted_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var requests = new List<CaptureRequestDto>
        {
            new()
            {
                SourceUrl = $"https://example.com/{Guid.NewGuid():N}",
                ContentType = ContentType.Article,
                RawContent = "Bulk one"
            },
            new()
            {
                SourceUrl = $"https://example.com/{Guid.NewGuid():N}",
                ContentType = ContentType.Note,
                RawContent = "Bulk two"
            }
        };

        var response = await client.PostAsJsonAsync("/api/v1/capture/bulk", requests);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
        var accepted = await response.Content.ReadFromJsonAsync<List<CaptureAcceptedDto>>();
        accepted.Should().NotBeNull();
        accepted!.Should().HaveCount(2);
        accepted.Should().OnlyContain(item => item.Id != Guid.Empty);
    }

    [Fact]
    public async Task CreateCapturesBulk_WithInvalidItem_ShouldReturn400_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var requests = new List<CaptureRequestDto>
        {
            new()
            {
                SourceUrl = $"https://example.com/{Guid.NewGuid():N}",
                ContentType = ContentType.Article,
                RawContent = "Bulk one"
            },
            new()
            {
                SourceUrl = "not-a-valid-url",
                ContentType = ContentType.Article,
                RawContent = "Bulk two"
            }
        };

        var response = await client.PostAsJsonAsync("/api/v1/capture/bulk", requests);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCapture_ShouldPersistExplicitLabels_ForOwner()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var request = new CaptureRequestDto
        {
            SourceUrl = $"https://example.com/{Guid.NewGuid():N}",
            ContentType = ContentType.Article,
            RawContent = "Labeled content",
            Labels =
            [
                new LabelAssignmentDto { Category = "Language", Value = "English" },
                new LabelAssignmentDto { Category = "Source", Value = "Web" }
            ]
        };

        var response = await client.PostAsJsonAsync("/api/v1/capture", request);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);

        var accepted = await response.Content.ReadFromJsonAsync<CaptureAcceptedDto>();
        accepted.Should().NotBeNull();

        var capture = await client.GetFromJsonAsync<CaptureResponseDto>(
            $"/api/v1/capture/{accepted!.Id}",
            ResponseJsonOptions);

        capture.Should().NotBeNull();
        capture!.Labels.Should().Contain(label => label.Category == "Language" && label.Value == "English");
        capture.Labels.Should().Contain(label => label.Category == "Source" && label.Value == "Web");
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
    public async Task GetCapture_ShouldReturnRawContentAndMetadata_ForOwner()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var request = new CaptureRequestDto
        {
            SourceUrl = $"https://example.com/{Guid.NewGuid():N}",
            ContentType = ContentType.Article,
            RawContent = "Stored raw content for capture detail.",
            Metadata = JsonSerializer.Serialize(new { author = "Integration Test", length = 42 }),
            Tags = new List<string> { "detail" }
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/capture", request);
        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);

        var accepted = await createResponse.Content.ReadFromJsonAsync<CaptureAcceptedDto>();
        accepted.Should().NotBeNull();

        var getResponse = await client.GetAsync($"/api/v1/capture/{accepted!.Id}");
        getResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var capture = await getResponse.Content.ReadFromJsonAsync<CaptureResponseDto>(ResponseJsonOptions);
        capture.Should().NotBeNull();
        capture!.RawContent.Should().Be(request.RawContent);
        capture.Metadata.Should().Be(request.Metadata);
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
    public async Task GetCaptureList_ShouldApplyFilteringSortingPaginationAndOwnerScope()
    {
        var owner = await _fixture.CreateMemberClientAsync();
        using var ownerClient = owner.Client;
        var foreign = await _fixture.CreateMemberClientAsync();
        using var foreignClient = foreign.Client;

        var ownerUserId = await _fixture.GetUserIdByEmailAsync(owner.Email);
        var foreignUserId = await _fixture.GetUserIdByEmailAsync(foreign.Email);
        var baseTime = new DateTime(2026, 3, 31, 8, 0, 0, DateTimeKind.Utc);

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.RawCaptures.AddRange(
                new RawCapture
                {
                    Id = Guid.NewGuid(),
                    OwnerUserId = ownerUserId,
                    SourceUrl = "https://example.com/z-last",
                    ContentType = ContentType.Article,
                    RawContent = "Owner article 1",
                    Status = CaptureStatus.Pending,
                    CreatedAt = baseTime.AddMinutes(1)
                },
                new RawCapture
                {
                    Id = Guid.NewGuid(),
                    OwnerUserId = ownerUserId,
                    SourceUrl = "https://example.com/a-first",
                    ContentType = ContentType.Article,
                    RawContent = "Owner article 2",
                    Status = CaptureStatus.Pending,
                    CreatedAt = baseTime.AddMinutes(2),
                    Metadata = """{"lastProcessingError":"stale error"}"""
                },
                new RawCapture
                {
                    Id = Guid.NewGuid(),
                    OwnerUserId = ownerUserId,
                    SourceUrl = "https://example.com/note",
                    ContentType = ContentType.Note,
                    RawContent = "Owner note",
                    Status = CaptureStatus.Completed,
                    CreatedAt = baseTime.AddMinutes(3)
                },
                new RawCapture
                {
                    Id = Guid.NewGuid(),
                    OwnerUserId = foreignUserId,
                    SourceUrl = "https://example.com/foreign",
                    ContentType = ContentType.Article,
                    RawContent = "Foreign owner article",
                    Status = CaptureStatus.Pending,
                    CreatedAt = baseTime.AddMinutes(4)
                });

            await Task.CompletedTask;
        });

        var response = await ownerClient.GetAsync(
            "/api/v1/capture/list?page=1&pageSize=1&sortField=sourceUrl&sortDirection=asc&contentType=Article&status=Pending");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<CaptureListPageDto>(ResponseJsonOptions);
        page.Should().NotBeNull();
        page!.TotalCount.Should().Be(2);
        page.Page.Should().Be(1);
        page.PageSize.Should().Be(1);
        page.Items.Should().ContainSingle();
        page.Items[0].SourceUrl.Should().Be("https://example.com/a-first");
        page.Items[0].ContentType.Should().Be(ContentType.Article);
        page.Items[0].Status.Should().Be(CaptureStatus.Pending);
        page.Items[0].FailureReason.Should().Be("stale error");

        var foreignResponse = await foreignClient.GetAsync(
            "/api/v1/capture/list?page=1&pageSize=10&contentType=Article&status=Pending");

        foreignResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var foreignPage = await foreignResponse.Content.ReadFromJsonAsync<CaptureListPageDto>(ResponseJsonOptions);
        foreignPage.Should().NotBeNull();
        foreignPage!.TotalCount.Should().Be(1);
        foreignPage.Items.Should().ContainSingle();
        foreignPage.Items[0].SourceUrl.Should().Be("https://example.com/foreign");
    }

    [Fact]
    public async Task GetCaptureList_ShouldReturn400_WhenFilterInvalid()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v1/capture/list?status=definitely-invalid");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
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
        var ownerCapture = await ownerGetResponse.Content.ReadFromJsonAsync<CaptureResponseDto>(ResponseJsonOptions);
        ownerCapture.Should().NotBeNull();
        ownerCapture!.RawContent.Should().Be("Owner scoped capture content.");
    }

    [Fact]
    public async Task CreateCapture_ShouldAllowDirectContentWithoutUrl_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var request = new CaptureRequestDto
        {
            SourceUrl = string.Empty,
            ContentType = ContentType.Note,
            RawContent = "Manual frontend capture without a URL.",
            Metadata = JsonSerializer.Serialize(new { source = "frontend_manual_input" })
        };

        var response = await client.PostAsJsonAsync("/api/v1/capture", request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
        var accepted = await response.Content.ReadFromJsonAsync<CaptureAcceptedDto>();
        accepted.Should().NotBeNull();

        var detail = await client.GetFromJsonAsync<CaptureResponseDto>($"/api/v1/capture/{accepted!.Id}", ResponseJsonOptions);
        detail.Should().NotBeNull();
        detail!.SourceUrl.Should().BeEmpty();
        detail.RawContent.Should().Be(request.RawContent);
    }

    [Fact]
    public async Task GetCapture_ShouldIncludeTopicLink_WhenProcessedInsightIsClustered()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var ownerUserId = await _fixture.GetUserIdByEmailAsync(IntegrationTestFixture.BootstrapAdminEmail);
        var captureId = Guid.NewGuid();
        var insightId = Guid.NewGuid();
        var clusterId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.RawCaptures.Add(new RawCapture
            {
                Id = captureId,
                OwnerUserId = ownerUserId,
                SourceUrl = "https://example.com/topic-capture",
                ContentType = ContentType.Article,
                RawContent = "Topic capture detail",
                Status = CaptureStatus.Completed,
                CreatedAt = now,
                ProcessedAt = now.AddMinutes(1)
            });

            dbContext.ProcessedInsights.Add(new ProcessedInsight
            {
                Id = insightId,
                OwnerUserId = ownerUserId,
                RawCaptureId = captureId,
                Title = "Clustered insight",
                Summary = "Clustered summary",
                KeyInsights = JsonSerializer.Serialize(new[] { "Insight" }),
                ActionItems = JsonSerializer.Serialize(new[] { "Action" }),
                ProcessedAt = now.AddMinutes(1)
            });

            dbContext.InsightClusters.Add(new InsightCluster
            {
                Id = clusterId,
                OwnerUserId = ownerUserId,
                Title = "AI Infrastructure",
                Description = "Cluster description",
                KeywordsJson = JsonSerializer.Serialize(new[] { "ai", "infra", "ops" }),
                MemberCount = 1,
                RepresentativeProcessedInsightId = insightId,
                LastComputedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });

            dbContext.InsightClusterMemberships.Add(new InsightClusterMembership
            {
                InsightClusterId = clusterId,
                ProcessedInsightId = insightId,
                Rank = 1,
                SimilarityToCentroid = 0.99,
                CreatedAt = now
            });

            await Task.CompletedTask;
        });

        var response = await client.GetAsync($"/api/v1/capture/{captureId}");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var capture = await response.Content.ReadFromJsonAsync<CaptureResponseDto>(ResponseJsonOptions);
        capture.Should().NotBeNull();
        capture!.ProcessedInsight.Should().NotBeNull();
        capture.ProcessedInsight!.Cluster.Should().NotBeNull();
        capture.ProcessedInsight.Cluster!.Id.Should().Be(clusterId);
        capture.ProcessedInsight.Cluster.SuggestedLabel.Category.Should().Be("Topic");
    }
}
