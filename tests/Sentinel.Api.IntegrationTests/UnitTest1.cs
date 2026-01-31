using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sentinel.Application.Dtos;
using Sentinel.Domain.Enums;

namespace Sentinel.Api.IntegrationTests;

public sealed class CaptureFlowTests : IClassFixture<ApiTestFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public CaptureFlowTests(ApiTestFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task CaptureFlow_ReturnsProcessedInsight()
    {
        var captureRequest = new CaptureRequest
        {
            SourceId = "tweet-123",
            Source = CaptureSource.XCom,
            RawText = "Great growth tactics for SaaS marketing. Thread 1/3 https://example.com/article?utm=1",
            Url = "https://x.com/author/status/123",
            AuthorHandle = "author",
            CapturedAt = DateTimeOffset.UtcNow
        };

        var captureResponse = await _client.PostAsJsonAsync("/api/v1/capture", captureRequest, JsonOptions);
        Assert.Equal(System.Net.HttpStatusCode.Created, captureResponse.StatusCode);

        var capture = await captureResponse.Content.ReadFromJsonAsync<CaptureResponse>(JsonOptions);
        Assert.NotNull(capture);

        var detailsResponse = await _client.GetAsync($"/api/v1/capture/{capture!.CaptureId}");
        Assert.Equal(System.Net.HttpStatusCode.OK, detailsResponse.StatusCode);

        var details = await detailsResponse.Content.ReadFromJsonAsync<CaptureDetailsResponse>(JsonOptions);
        Assert.NotNull(details);
        Assert.Equal(ProcessingStatus.Processed, details!.Status);
        Assert.NotNull(details.Insight);
        Assert.NotEmpty(details.Insight!.Tags);

        var semanticResponse = await _client.PostAsJsonAsync(
            "/api/v1/search/semantic",
            new SemanticSearchRequest { Query = "growth", Limit = 5 },
            JsonOptions);
        Assert.Equal(System.Net.HttpStatusCode.OK, semanticResponse.StatusCode);

        var semanticResults = await semanticResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<ProcessedInsightDto>>(JsonOptions);
        Assert.NotNull(semanticResults);
        Assert.NotEmpty(semanticResults!);

        var tag = details.Insight.Tags.First();
        var tagsResponse = await _client.PostAsJsonAsync(
            "/api/v1/search/tags",
            new TagSearchRequest { Tags = new[] { tag }, MatchAll = true, Limit = 5 },
            JsonOptions);
        Assert.Equal(System.Net.HttpStatusCode.OK, tagsResponse.StatusCode);

        var tagResults = await tagsResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<ProcessedInsightDto>>(JsonOptions);
        Assert.NotNull(tagResults);
        Assert.NotEmpty(tagResults!);
    }
}
