using AwesomeAssertions;
using Pgvector;
using SentinelKnowledgebase.Application.DTOs.Labels;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Application.DTOs.Search;
using System.Net.Http.Json;
using Xunit;

namespace SentinelKnowledgebase.IntegrationTests;

[Collection("IntegrationTests")]
public class SearchControllerTests
{
    private readonly IntegrationTestFixture _fixture;
    
    public SearchControllerTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task SemanticSearch_ShouldReturn401_WhenAnonymous()
    {
        using var client = _fixture.CreateClient();

        var request = new SemanticSearchRequestDto
        {
            Query = "test query",
            TopK = 5,
            Threshold = 0.5
        };
        
        var response = await client.PostAsJsonAsync("/api/v1/search/semantic", request);
        
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SemanticSearch_ShouldReturnEmptyList_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var request = new SemanticSearchRequestDto
        {
            Query = "test query",
            TopK = 5,
            Threshold = 0.5
        };
        
        var response = await client.PostAsJsonAsync("/api/v1/search/semantic", request);
        
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<SemanticSearchResultDto>>();
        content.Should().NotBeNull();
        content.Should().BeEmpty();
    }
    
    [Fact]
    public async Task TagSearch_ShouldReturnEmptyList_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var request = new TagSearchRequestDto
        {
            Tags = new List<string> { "nonexistent-tag" },
            MatchAll = false
        };
        
        var response = await client.PostAsJsonAsync("/api/v1/search/tags", request);
        
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<TagSearchResultDto>>();
        content.Should().NotBeNull();
        content.Should().BeEmpty();
    }
    
    [Fact]
    public async Task SemanticSearch_WithEmptyQuery_ShouldReturn400_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var request = new SemanticSearchRequestDto
        {
            Query = "",
            TopK = 5
        };
        
        var response = await client.PostAsJsonAsync("/api/v1/search/semantic", request);
        
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task TagSearch_WithEmptyTags_ShouldReturn400_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var request = new TagSearchRequestDto
        {
            Tags = new List<string>()
        };
        
        var response = await client.PostAsJsonAsync("/api/v1/search/tags", request);
        
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LabelSearch_ShouldReturnEmptyList_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var request = new LabelSearchRequestDto
        {
            Labels =
            [
                new LabelAssignmentDto { Category = "Language", Value = "Nonexistent" }
            ]
        };

        var response = await client.PostAsJsonAsync("/api/v1/search/labels", request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<LabelSearchResultDto>>();
        content.Should().NotBeNull();
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task LabelSearch_WithEmptyLabels_ShouldReturn400_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var request = new LabelSearchRequestDto
        {
            Labels = []
        };

        var response = await client.PostAsJsonAsync("/api/v1/search/labels", request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SemanticSearch_ShouldReturnOnlyOwnerResults_WhenDifferentUsersHaveData()
    {
        using var adminClient = await _fixture.CreateAuthenticatedClientAsync();
        var member = await _fixture.CreateMemberClientAsync();
        using var memberClient = member.Client;

        var adminUserId = await _fixture.GetUserIdByEmailAsync(IntegrationTestFixture.BootstrapAdminEmail);
        var memberUserId = await _fixture.GetUserIdByEmailAsync(member.Email);

        var adminInsightId = Guid.NewGuid();
        var memberInsightId = Guid.NewGuid();
        var embedding = new Vector(CreateDeterministicEmbedding());

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            var adminCapture = new RawCapture
            {
                Id = Guid.NewGuid(),
                OwnerUserId = adminUserId,
                SourceUrl = $"https://example.com/admin-search/{Guid.NewGuid():N}",
                ContentType = Domain.Enums.ContentType.Article,
                RawContent = "Admin semantic search content.",
                Status = Domain.Enums.CaptureStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            var memberCapture = new RawCapture
            {
                Id = Guid.NewGuid(),
                OwnerUserId = memberUserId,
                SourceUrl = $"https://example.com/member-search/{Guid.NewGuid():N}",
                ContentType = Domain.Enums.ContentType.Article,
                RawContent = "Member semantic search content.",
                Status = Domain.Enums.CaptureStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            var adminInsight = new ProcessedInsight
            {
                Id = adminInsightId,
                OwnerUserId = adminUserId,
                RawCaptureId = adminCapture.Id,
                Title = "Admin Insight",
                Summary = "Admin summary",
                ProcessedAt = DateTime.UtcNow
            };

            var memberInsight = new ProcessedInsight
            {
                Id = memberInsightId,
                OwnerUserId = memberUserId,
                RawCaptureId = memberCapture.Id,
                Title = "Member Insight",
                Summary = "Member summary",
                ProcessedAt = DateTime.UtcNow
            };

            dbContext.RawCaptures.AddRange(adminCapture, memberCapture);
            dbContext.ProcessedInsights.AddRange(adminInsight, memberInsight);
            dbContext.EmbeddingVectors.AddRange(
                new EmbeddingVector
                {
                    Id = Guid.NewGuid(),
                    ProcessedInsightId = adminInsightId,
                    Vector = embedding
                },
                new EmbeddingVector
                {
                    Id = Guid.NewGuid(),
                    ProcessedInsightId = memberInsightId,
                    Vector = embedding
                });

            await Task.CompletedTask;
        });

        var request = new SemanticSearchRequestDto
        {
            Query = "owned search query",
            TopK = 10,
            Threshold = 0.5
        };

        var adminResponse = await adminClient.PostAsJsonAsync("/api/v1/search/semantic", request);
        adminResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var adminResults = await adminResponse.Content.ReadFromJsonAsync<List<SemanticSearchResultDto>>();
        adminResults.Should().NotBeNull();
        adminResults!.Should().ContainSingle(result => result.Id == adminInsightId);
        adminResults.Should().NotContain(result => result.Id == memberInsightId);

        var memberResponse = await memberClient.PostAsJsonAsync("/api/v1/search/semantic", request);
        memberResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var memberResults = await memberResponse.Content.ReadFromJsonAsync<List<SemanticSearchResultDto>>();
        memberResults.Should().NotBeNull();
        memberResults!.Should().ContainSingle(result => result.Id == memberInsightId);
        memberResults.Should().NotContain(result => result.Id == adminInsightId);
    }

    [Fact]
    public async Task TagSearch_ShouldReturnOnlyOwnerResults_WhenDifferentUsersReuseSameTag()
    {
        using var adminClient = await _fixture.CreateAuthenticatedClientAsync();
        var member = await _fixture.CreateMemberClientAsync();
        using var memberClient = member.Client;

        var adminUserId = await _fixture.GetUserIdByEmailAsync(IntegrationTestFixture.BootstrapAdminEmail);
        var memberUserId = await _fixture.GetUserIdByEmailAsync(member.Email);
        var sharedTag = $"search-tag-{Guid.NewGuid():N}";
        var adminInsightId = Guid.NewGuid();
        var memberInsightId = Guid.NewGuid();

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            var adminTag = new Tag
            {
                Id = Guid.NewGuid(),
                OwnerUserId = adminUserId,
                Name = sharedTag,
                CreatedAt = DateTime.UtcNow
            };

            var memberTag = new Tag
            {
                Id = Guid.NewGuid(),
                OwnerUserId = memberUserId,
                Name = sharedTag,
                CreatedAt = DateTime.UtcNow
            };

            var adminCapture = new RawCapture
            {
                Id = Guid.NewGuid(),
                OwnerUserId = adminUserId,
                SourceUrl = $"https://example.com/admin-tag-search/{Guid.NewGuid():N}",
                ContentType = Domain.Enums.ContentType.Article,
                RawContent = "Admin tag search content.",
                Status = Domain.Enums.CaptureStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow,
                Tags = new List<Tag> { adminTag }
            };

            var memberCapture = new RawCapture
            {
                Id = Guid.NewGuid(),
                OwnerUserId = memberUserId,
                SourceUrl = $"https://example.com/member-tag-search/{Guid.NewGuid():N}",
                ContentType = Domain.Enums.ContentType.Article,
                RawContent = "Member tag search content.",
                Status = Domain.Enums.CaptureStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow,
                Tags = new List<Tag> { memberTag }
            };

            var adminInsight = new ProcessedInsight
            {
                Id = adminInsightId,
                OwnerUserId = adminUserId,
                RawCaptureId = adminCapture.Id,
                Title = "Admin Tag Insight",
                Summary = "Admin tag summary",
                ProcessedAt = DateTime.UtcNow,
                Tags = new List<Tag> { adminTag }
            };

            var memberInsight = new ProcessedInsight
            {
                Id = memberInsightId,
                OwnerUserId = memberUserId,
                RawCaptureId = memberCapture.Id,
                Title = "Member Tag Insight",
                Summary = "Member tag summary",
                ProcessedAt = DateTime.UtcNow,
                Tags = new List<Tag> { memberTag }
            };

            dbContext.RawCaptures.AddRange(adminCapture, memberCapture);
            dbContext.ProcessedInsights.AddRange(adminInsight, memberInsight);
            await Task.CompletedTask;
        });

        var request = new TagSearchRequestDto
        {
            Tags = new List<string> { sharedTag },
            MatchAll = false
        };

        var adminResponse = await adminClient.PostAsJsonAsync("/api/v1/search/tags", request);
        adminResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var adminResults = await adminResponse.Content.ReadFromJsonAsync<List<TagSearchResultDto>>();
        adminResults.Should().NotBeNull();
        adminResults!.Should().ContainSingle(result => result.Id == adminInsightId);
        adminResults.Should().NotContain(result => result.Id == memberInsightId);

        var memberResponse = await memberClient.PostAsJsonAsync("/api/v1/search/tags", request);
        memberResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var memberResults = await memberResponse.Content.ReadFromJsonAsync<List<TagSearchResultDto>>();
        memberResults.Should().NotBeNull();
        memberResults!.Should().ContainSingle(result => result.Id == memberInsightId);
        memberResults.Should().NotContain(result => result.Id == adminInsightId);
    }

    [Fact]
    public async Task LabelSearch_ShouldReturnOnlyOwnerResults_WhenDifferentUsersReuseSameLabel()
    {
        using var adminClient = await _fixture.CreateAuthenticatedClientAsync();
        var member = await _fixture.CreateMemberClientAsync();
        using var memberClient = member.Client;

        var adminUserId = await _fixture.GetUserIdByEmailAsync(IntegrationTestFixture.BootstrapAdminEmail);
        var memberUserId = await _fixture.GetUserIdByEmailAsync(member.Email);
        var adminInsightId = Guid.NewGuid();
        var memberInsightId = Guid.NewGuid();

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            var adminCategory = new LabelCategory
            {
                Id = Guid.NewGuid(),
                OwnerUserId = adminUserId,
                Name = "Language"
            };
            var memberCategory = new LabelCategory
            {
                Id = Guid.NewGuid(),
                OwnerUserId = memberUserId,
                Name = "Language"
            };
            var adminValue = new LabelValue
            {
                Id = Guid.NewGuid(),
                LabelCategoryId = adminCategory.Id,
                LabelCategory = adminCategory,
                Value = "English"
            };
            var memberValue = new LabelValue
            {
                Id = Guid.NewGuid(),
                LabelCategoryId = memberCategory.Id,
                LabelCategory = memberCategory,
                Value = "English"
            };

            var adminCapture = new RawCapture
            {
                Id = Guid.NewGuid(),
                OwnerUserId = adminUserId,
                SourceUrl = $"https://example.com/admin-label-search/{Guid.NewGuid():N}",
                ContentType = Domain.Enums.ContentType.Article,
                RawContent = "Admin label search content.",
                Status = Domain.Enums.CaptureStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };
            adminCapture.LabelAssignments.Add(new RawCaptureLabelAssignment
            {
                RawCaptureId = adminCapture.Id,
                LabelCategoryId = adminCategory.Id,
                LabelCategory = adminCategory,
                LabelValueId = adminValue.Id,
                LabelValue = adminValue
            });

            var memberCapture = new RawCapture
            {
                Id = Guid.NewGuid(),
                OwnerUserId = memberUserId,
                SourceUrl = $"https://example.com/member-label-search/{Guid.NewGuid():N}",
                ContentType = Domain.Enums.ContentType.Article,
                RawContent = "Member label search content.",
                Status = Domain.Enums.CaptureStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };
            memberCapture.LabelAssignments.Add(new RawCaptureLabelAssignment
            {
                RawCaptureId = memberCapture.Id,
                LabelCategoryId = memberCategory.Id,
                LabelCategory = memberCategory,
                LabelValueId = memberValue.Id,
                LabelValue = memberValue
            });

            var adminInsight = new ProcessedInsight
            {
                Id = adminInsightId,
                OwnerUserId = adminUserId,
                RawCaptureId = adminCapture.Id,
                Title = "Admin Label Insight",
                Summary = "Admin label summary",
                ProcessedAt = DateTime.UtcNow
            };
            adminInsight.LabelAssignments.Add(new ProcessedInsightLabelAssignment
            {
                ProcessedInsightId = adminInsightId,
                LabelCategoryId = adminCategory.Id,
                LabelCategory = adminCategory,
                LabelValueId = adminValue.Id,
                LabelValue = adminValue
            });

            var memberInsight = new ProcessedInsight
            {
                Id = memberInsightId,
                OwnerUserId = memberUserId,
                RawCaptureId = memberCapture.Id,
                Title = "Member Label Insight",
                Summary = "Member label summary",
                ProcessedAt = DateTime.UtcNow
            };
            memberInsight.LabelAssignments.Add(new ProcessedInsightLabelAssignment
            {
                ProcessedInsightId = memberInsightId,
                LabelCategoryId = memberCategory.Id,
                LabelCategory = memberCategory,
                LabelValueId = memberValue.Id,
                LabelValue = memberValue
            });

            dbContext.RawCaptures.AddRange(adminCapture, memberCapture);
            dbContext.ProcessedInsights.AddRange(adminInsight, memberInsight);
            await Task.CompletedTask;
        });

        var request = new LabelSearchRequestDto
        {
            Labels =
            [
                new LabelAssignmentDto { Category = "Language", Value = "English" }
            ]
        };

        var adminResponse = await adminClient.PostAsJsonAsync("/api/v1/search/labels", request);
        adminResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var adminResults = await adminResponse.Content.ReadFromJsonAsync<List<LabelSearchResultDto>>();
        adminResults.Should().NotBeNull();
        adminResults!.Should().ContainSingle(result => result.Id == adminInsightId);
        adminResults.Should().NotContain(result => result.Id == memberInsightId);

        var memberResponse = await memberClient.PostAsJsonAsync("/api/v1/search/labels", request);
        memberResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var memberResults = await memberResponse.Content.ReadFromJsonAsync<List<LabelSearchResultDto>>();
        memberResults.Should().NotBeNull();
        memberResults!.Should().ContainSingle(result => result.Id == memberInsightId);
        memberResults.Should().NotContain(result => result.Id == adminInsightId);
    }

    private static float[] CreateDeterministicEmbedding()
    {
        var random = new Random(42);
        var vector = new float[1536];
        for (var index = 0; index < vector.Length; index++)
        {
            vector[index] = (float)random.NextDouble() * 2 - 1;
        }

        var norm = (float)Math.Sqrt(vector.Sum(value => value * value));
        return norm > 0
            ? vector.Select(value => value / norm).ToArray()
            : vector;
    }
}
