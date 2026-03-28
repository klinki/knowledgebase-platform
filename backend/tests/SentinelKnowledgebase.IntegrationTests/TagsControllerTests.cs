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

    [Fact]
    public async Task GetTags_ShouldReturnUnusedTags_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var uniqueTag = $"unused-tag-{Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("/api/v1/tags", new { name = uniqueTag });

        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var tagsResponse = await client.GetAsync("/api/v1/tags");

        tagsResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var tags = await tagsResponse.Content.ReadFromJsonAsync<List<TagSummaryDto>>();
        tags.Should().NotBeNull();
        tags!.Should().Contain(tag => tag.Name == uniqueTag && tag.Count == 0 && tag.LastUsedAt == null);
    }

    [Fact]
    public async Task GetTags_ShouldBeUserScoped_WhenDifferentUsersReuseSameTagName()
    {
        using var adminClient = await _fixture.CreateAuthenticatedClientAsync();
        var member = await _fixture.CreateMemberClientAsync();
        using var memberClient = member.Client;
        var sharedTag = $"shared-{Guid.NewGuid():N}";

        var adminCreateResponse = await adminClient.PostAsJsonAsync("/api/v1/capture", new CaptureRequestDto
        {
            SourceUrl = $"https://example.com/admin-tags/{Guid.NewGuid():N}",
            ContentType = ContentType.Article,
            RawContent = "Admin tag ownership capture.",
            Metadata = JsonSerializer.Serialize(new { source = "admin" }),
            Tags = new List<string> { sharedTag }
        });
        adminCreateResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);

        var memberCreateResponse = await memberClient.PostAsJsonAsync("/api/v1/capture", new CaptureRequestDto
        {
            SourceUrl = $"https://example.com/member-tags/{Guid.NewGuid():N}",
            ContentType = ContentType.Article,
            RawContent = "Member tag ownership capture.",
            Metadata = JsonSerializer.Serialize(new { source = "member" }),
            Tags = new List<string> { sharedTag }
        });
        memberCreateResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);

        var adminTagsResponse = await adminClient.GetAsync("/api/v1/tags");
        adminTagsResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var adminTags = await adminTagsResponse.Content.ReadFromJsonAsync<List<TagSummaryDto>>();
        adminTags.Should().NotBeNull();
        adminTags!.Should().Contain(tag => tag.Name == sharedTag && tag.Count == 1);

        var memberTagsResponse = await memberClient.GetAsync("/api/v1/tags");
        memberTagsResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var memberTags = await memberTagsResponse.Content.ReadFromJsonAsync<List<TagSummaryDto>>();
        memberTags.Should().NotBeNull();
        memberTags!.Should().Contain(tag => tag.Name == sharedTag && tag.Count == 1);
    }

    [Fact]
    public async Task GetTags_ShouldReuseSingleOwnerTag_WhenTrimmedNamesMatch()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var baseTag = $"trimmed-{Guid.NewGuid():N}";

        var firstResponse = await client.PostAsJsonAsync("/api/v1/capture", new CaptureRequestDto
        {
            SourceUrl = $"https://example.com/tags/trimmed/{Guid.NewGuid():N}/1",
            ContentType = ContentType.Article,
            RawContent = "First trimmed tag capture.",
            Metadata = JsonSerializer.Serialize(new { source = "integration-test" }),
            Tags = new List<string> { $"  {baseTag}  " }
        });
        firstResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);

        var secondResponse = await client.PostAsJsonAsync("/api/v1/capture", new CaptureRequestDto
        {
            SourceUrl = $"https://example.com/tags/trimmed/{Guid.NewGuid():N}/2",
            ContentType = ContentType.Article,
            RawContent = "Second trimmed tag capture.",
            Metadata = JsonSerializer.Serialize(new { source = "integration-test" }),
            Tags = new List<string> { baseTag }
        });
        secondResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);

        var tagsResponse = await client.GetAsync("/api/v1/tags");
        tagsResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var tags = await tagsResponse.Content.ReadFromJsonAsync<List<TagSummaryDto>>();
        tags.Should().NotBeNull();

        var matchingTags = tags!.Where(tag => tag.Name == baseTag).ToList();
        matchingTags.Should().ContainSingle();
        matchingTags[0].Count.Should().Be(2);
    }

    // ──── POST /api/v1/tags ────────────────────────────────────────────

    [Fact]
    public async Task CreateTag_ShouldReturn401_WhenAnonymous()
    {
        using var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/tags", new { name = "anon-tag" });

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTag_ShouldReturn201WithDto_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var tagName = $"new-tag-{Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("/api/v1/tags", new { name = tagName });

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var tag = await response.Content.ReadFromJsonAsync<TagSummaryDto>();
        tag.Should().NotBeNull();
        tag!.Name.Should().Be(tagName);
        tag.Id.Should().NotBe(Guid.Empty);
        tag.Count.Should().Be(0);
    }

    [Fact]
    public async Task CreateTag_ShouldReturn409_WhenNameAlreadyExists()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var tagName = $"dup-tag-{Guid.NewGuid():N}";

        var first = await client.PostAsJsonAsync("/api/v1/tags", new { name = tagName });
        first.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/v1/tags", new { name = tagName });
        second.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    // ──── PATCH /api/v1/tags/{id} ──────────────────────────────────────

    [Fact]
    public async Task RenameTag_ShouldReturn200WithUpdatedDto_WhenAuthenticated()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var original = $"rename-original-{Guid.NewGuid():N}";
        var renamed = $"rename-updated-{Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("/api/v1/tags", new { name = original });
        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<TagSummaryDto>();

        var patchResponse = await client.PatchAsJsonAsync($"/api/v1/tags/{created!.Id}", new { name = renamed });

        patchResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var updated = await patchResponse.Content.ReadFromJsonAsync<TagSummaryDto>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be(renamed);
        updated.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task RenameTag_ShouldReturn404_WhenTagDoesNotExist()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PatchAsJsonAsync($"/api/v1/tags/{Guid.NewGuid()}", new { name = "ghost" });

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RenameTag_ShouldReturn409_WhenNewNameConflicts()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var firstName = $"conflict-a-{Guid.NewGuid():N}";
        var secondName = $"conflict-b-{Guid.NewGuid():N}";

        var first = await client.PostAsJsonAsync("/api/v1/tags", new { name = firstName });
        var second = await client.PostAsJsonAsync("/api/v1/tags", new { name = secondName });
        first.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        second.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var secondTag = await second.Content.ReadFromJsonAsync<TagSummaryDto>();

        var response = await client.PatchAsJsonAsync($"/api/v1/tags/{secondTag!.Id}", new { name = firstName });

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    // ──── DELETE /api/v1/tags/{id} ─────────────────────────────────────

    [Fact]
    public async Task DeleteTag_ShouldReturn204_WhenTagExists()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var tagName = $"delete-me-{Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("/api/v1/tags", new { name = tagName });
        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<TagSummaryDto>();

        var deleteResponse = await client.DeleteAsync($"/api/v1/tags/{created!.Id}");

        deleteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteTag_ShouldReturn404_WhenTagDoesNotExist()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.DeleteAsync($"/api/v1/tags/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTag_ShouldReturn401_WhenAnonymous()
    {
        using var client = _fixture.CreateClient();

        var response = await client.DeleteAsync($"/api/v1/tags/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }
}
