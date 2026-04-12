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
    public async Task Search_ShouldReturn401_WhenAnonymous()
    {
        using var client = _fixture.CreateClient();

        var request = new SearchRequestDto
        {
            Query = "search"
        };

        var response = await client.PostAsJsonAsync("/api/v1/search", request);

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
    public async Task Search_WithEmptyCriteria_ShouldReturn400_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var request = new SearchRequestDto
        {
            Query = "   ",
            Tags = [],
            Labels = []
        };

        var response = await client.PostAsJsonAsync("/api/v1/search", request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_WithInvalidPage_ShouldReturn400_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var request = new SearchRequestDto
        {
            Query = "search",
            Page = 0
        };

        var response = await client.PostAsJsonAsync("/api/v1/search", request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_WithInvalidPageSize_ShouldReturn400_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var request = new SearchRequestDto
        {
            Query = "search",
            PageSize = 101
        };

        var response = await client.PostAsJsonAsync("/api/v1/search", request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
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
        const string query = "owned search query";
        var embedding = new Vector(CreateDeterministicEmbedding(query));

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
            Query = query,
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

    [Fact]
    public async Task Search_ShouldCombineSemanticAndTagFilters_ForAuthenticatedUser()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var adminUserId = await _fixture.GetUserIdByEmailAsync(IntegrationTestFixture.BootstrapAdminEmail);

        var matchingInsightId = Guid.NewGuid();
        var excludedInsightId = Guid.NewGuid();
        const string query = "combined search query";
        var sharedEmbedding = new Vector(CreateDeterministicEmbedding(query));
        var keepTag = $"keep-{Guid.NewGuid():N}";
        var dropTag = $"drop-{Guid.NewGuid():N}";

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            var keep = new Tag
            {
                Id = Guid.NewGuid(),
                OwnerUserId = adminUserId,
                Name = keepTag,
                CreatedAt = DateTime.UtcNow
            };
            var drop = new Tag
            {
                Id = Guid.NewGuid(),
                OwnerUserId = adminUserId,
                Name = dropTag,
                CreatedAt = DateTime.UtcNow
            };

            var matchingCapture = new RawCapture
            {
                Id = Guid.NewGuid(),
                OwnerUserId = adminUserId,
                SourceUrl = $"https://example.com/combined-match/{Guid.NewGuid():N}",
                ContentType = Domain.Enums.ContentType.Article,
                RawContent = "Combined search matching capture.",
                Status = Domain.Enums.CaptureStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            var excludedCapture = new RawCapture
            {
                Id = Guid.NewGuid(),
                OwnerUserId = adminUserId,
                SourceUrl = $"https://example.com/combined-drop/{Guid.NewGuid():N}",
                ContentType = Domain.Enums.ContentType.Article,
                RawContent = "Combined search excluded capture.",
                Status = Domain.Enums.CaptureStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            var matchingInsight = new ProcessedInsight
            {
                Id = matchingInsightId,
                OwnerUserId = adminUserId,
                RawCaptureId = matchingCapture.Id,
                Title = "Matching Combined Insight",
                Summary = "Should remain after filtering",
                ProcessedAt = DateTime.UtcNow,
                Tags = [keep]
            };

            var excludedInsight = new ProcessedInsight
            {
                Id = excludedInsightId,
                OwnerUserId = adminUserId,
                RawCaptureId = excludedCapture.Id,
                Title = "Excluded Combined Insight",
                Summary = "Should be filtered out",
                ProcessedAt = DateTime.UtcNow,
                Tags = [drop]
            };

            dbContext.RawCaptures.AddRange(matchingCapture, excludedCapture);
            dbContext.ProcessedInsights.AddRange(matchingInsight, excludedInsight);
            dbContext.EmbeddingVectors.AddRange(
                new EmbeddingVector
                {
                    Id = Guid.NewGuid(),
                    ProcessedInsightId = matchingInsightId,
                    Vector = sharedEmbedding
                },
                new EmbeddingVector
                {
                    Id = Guid.NewGuid(),
                    ProcessedInsightId = excludedInsightId,
                    Vector = sharedEmbedding
                });

            await Task.CompletedTask;
        });

        var request = new SearchRequestDto
        {
            Query = query,
            Tags = [keepTag],
            TagMatchMode = SearchMatchModes.All,
            Page = 1,
            PageSize = 10,
            Threshold = 0.5
        };

        var response = await client.PostAsJsonAsync("/api/v1/search", request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<SearchResultPageDto>();
        results.Should().NotBeNull();
        results!.TotalCount.Should().Be(1);
        results.Page.Should().Be(1);
        results.PageSize.Should().Be(10);
        results.Items.Should().ContainSingle(result => result.Id == matchingInsightId);
        results.Items.Should().NotContain(result => result.Id == excludedInsightId);
        results.Items.Single().Similarity.Should().NotBeNull();
    }

    [Fact]
    public async Task Search_ShouldReturnLabelOnlyResults_InProcessedAtOrder()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var adminUserId = await _fixture.GetUserIdByEmailAsync(IntegrationTestFixture.BootstrapAdminEmail);
        var olderInsightId = Guid.NewGuid();
        var newerInsightId = Guid.NewGuid();
        var categoryName = $"Language-{Guid.NewGuid():N}";
        var labelValue = $"English-{Guid.NewGuid():N}";

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            var category = new LabelCategory
            {
                Id = Guid.NewGuid(),
                OwnerUserId = adminUserId,
                Name = categoryName
            };
            var value = new LabelValue
            {
                Id = Guid.NewGuid(),
                LabelCategoryId = category.Id,
                LabelCategory = category,
                Value = labelValue
            };

            var olderCapture = new RawCapture
            {
                Id = Guid.NewGuid(),
                OwnerUserId = adminUserId,
                SourceUrl = $"https://example.com/older/{Guid.NewGuid():N}",
                ContentType = Domain.Enums.ContentType.Article,
                RawContent = "Older label search content.",
                Status = Domain.Enums.CaptureStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                ProcessedAt = DateTime.UtcNow.AddDays(-2)
            };

            var newerCapture = new RawCapture
            {
                Id = Guid.NewGuid(),
                OwnerUserId = adminUserId,
                SourceUrl = $"https://example.com/newer/{Guid.NewGuid():N}",
                ContentType = Domain.Enums.ContentType.Article,
                RawContent = "Newer label search content.",
                Status = Domain.Enums.CaptureStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                ProcessedAt = DateTime.UtcNow.AddDays(-1)
            };

            var olderInsight = new ProcessedInsight
            {
                Id = olderInsightId,
                OwnerUserId = adminUserId,
                RawCaptureId = olderCapture.Id,
                Title = "Older Label Insight",
                Summary = "Older summary",
                ProcessedAt = DateTime.UtcNow.AddDays(-2)
            };
            olderInsight.LabelAssignments.Add(new ProcessedInsightLabelAssignment
            {
                ProcessedInsightId = olderInsightId,
                LabelCategoryId = category.Id,
                LabelCategory = category,
                LabelValueId = value.Id,
                LabelValue = value
            });

            var newerInsight = new ProcessedInsight
            {
                Id = newerInsightId,
                OwnerUserId = adminUserId,
                RawCaptureId = newerCapture.Id,
                Title = "Newer Label Insight",
                Summary = "Newer summary",
                ProcessedAt = DateTime.UtcNow.AddDays(-1)
            };
            newerInsight.LabelAssignments.Add(new ProcessedInsightLabelAssignment
            {
                ProcessedInsightId = newerInsightId,
                LabelCategoryId = category.Id,
                LabelCategory = category,
                LabelValueId = value.Id,
                LabelValue = value
            });

            dbContext.RawCaptures.AddRange(olderCapture, newerCapture);
            dbContext.ProcessedInsights.AddRange(olderInsight, newerInsight);
            await Task.CompletedTask;
        });

        var request = new SearchRequestDto
        {
            Labels = [new LabelAssignmentDto { Category = categoryName, Value = labelValue }],
            LabelMatchMode = SearchMatchModes.All
        };

        var response = await client.PostAsJsonAsync("/api/v1/search", request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<SearchResultPageDto>();
        results.Should().NotBeNull();
        results!.TotalCount.Should().Be(2);
        results.Items.Select(result => result.Id).Should().ContainInOrder(newerInsightId, olderInsightId);
        results.Items.Should().OnlyContain(result => result.Similarity == null);
    }

    [Fact]
    public async Task Search_ShouldReturnPagedSemanticResults_WhenPageAndPageSizeAreProvided()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var adminUserId = await _fixture.GetUserIdByEmailAsync(IntegrationTestFixture.BootstrapAdminEmail);
        const string query = "paged semantic search";
        var embedding = new Vector(CreateDeterministicEmbedding(query));
        Guid[] orderedInsightIds =
        [
            Guid.Parse("00000000-0000-0000-0000-000000000011"),
            Guid.Parse("00000000-0000-0000-0000-000000000022"),
            Guid.Parse("00000000-0000-0000-0000-000000000033")
        ];

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            foreach (var insightId in orderedInsightIds)
            {
                var capture = new RawCapture
                {
                    Id = Guid.NewGuid(),
                    OwnerUserId = adminUserId,
                    SourceUrl = $"https://example.com/paged-search/{insightId:N}",
                    ContentType = Domain.Enums.ContentType.Article,
                    RawContent = $"Semantic search content {insightId:N}.",
                    Status = Domain.Enums.CaptureStatus.Completed,
                    CreatedAt = DateTime.UtcNow,
                    ProcessedAt = DateTime.UtcNow
                };

                var insight = new ProcessedInsight
                {
                    Id = insightId,
                    OwnerUserId = adminUserId,
                    RawCaptureId = capture.Id,
                    Title = $"Paged Insight {insightId:N}",
                    Summary = "Summary",
                    ProcessedAt = DateTime.UtcNow
                };

                dbContext.RawCaptures.Add(capture);
                dbContext.ProcessedInsights.Add(insight);
                dbContext.EmbeddingVectors.Add(new EmbeddingVector
                {
                    Id = Guid.NewGuid(),
                    ProcessedInsightId = insightId,
                    Vector = embedding
                });
            }

            await Task.CompletedTask;
        });

        var request = new SearchRequestDto
        {
            Query = query,
            Page = 2,
            PageSize = 2,
            Threshold = 0.5
        };

        var response = await client.PostAsJsonAsync("/api/v1/search", request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<SearchResultPageDto>();
        results.Should().NotBeNull();
        results!.TotalCount.Should().Be(3);
        results.Page.Should().Be(2);
        results.PageSize.Should().Be(2);
        results.Items.Should().ContainSingle();
        results.Items.Single().Id.Should().Be(orderedInsightIds[2]);
    }

    private static float[] CreateDeterministicEmbedding(string text)
    {
        var seed = text.Aggregate(17, (current, character) => unchecked(current * 31 + character));
        var random = new Random(seed);
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
