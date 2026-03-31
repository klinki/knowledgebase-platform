using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Hangfire;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Application.DTOs.Dashboard;
using SentinelKnowledgebase.Domain.Enums;
using Xunit;

namespace SentinelKnowledgebase.IntegrationTests;

[Collection("IntegrationTests")]
public class AdminProcessingControllerTests
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IntegrationTestFixture _fixture;

    public AdminProcessingControllerTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetOverview_ShouldReturn401_WhenAnonymous()
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync("/api/v1/admin/processing");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOverview_ShouldReturn403_WhenNonAdmin()
    {
        var member = await _fixture.CreateMemberClientAsync();
        using var client = member.Client;

        var response = await client.GetAsync("/api/v1/admin/processing");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateCapture_ShouldRemainPendingAndReturnPausedMessage_WhenProcessingPaused()
    {
        using var adminClient = await _fixture.CreateAuthenticatedClientAsync();

        var pauseResponse = await adminClient.PostAsync("/api/v1/admin/processing/pause", null);
        pauseResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/capture", new CaptureRequestDto
        {
            SourceUrl = $"https://example.com/{Guid.NewGuid():N}",
            ContentType = ContentType.Article,
            RawContent = "Paused processing capture"
        });

        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
        var accepted = await createResponse.Content.ReadFromJsonAsync<CaptureAcceptedDto>();
        accepted.Should().NotBeNull();
        accepted!.Message.Should().Contain("paused");

        var detail = await adminClient.GetFromJsonAsync<CaptureResponseDto>(
            $"/api/v1/capture/{accepted.Id}",
            ResponseJsonOptions);

        detail.Should().NotBeNull();
        detail!.Status.Should().Be(CaptureStatus.Pending);
    }

    [Fact]
    public async Task Resume_ShouldEnqueuePendingCaptures()
    {
        using var adminClient = await _fixture.CreateAuthenticatedClientAsync();

        using var beforeScope = _fixture.CreateScope();
        var storage = beforeScope.ServiceProvider.GetRequiredService<JobStorage>();
        var beforeEnqueued = storage.GetMonitoringApi().EnqueuedCount("default");

        var pauseResponse = await adminClient.PostAsync("/api/v1/admin/processing/pause", null);
        pauseResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/capture", new CaptureRequestDto
        {
            SourceUrl = $"https://example.com/{Guid.NewGuid():N}",
            ContentType = ContentType.Article,
            RawContent = "Resume processing capture"
        });
        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);

        var resumeResponse = await adminClient.PostAsync("/api/v1/admin/processing/resume", null);
        resumeResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var overview = await resumeResponse.Content.ReadFromJsonAsync<CaptureProcessingAdminOverviewDto>(ResponseJsonOptions);
        overview.Should().NotBeNull();
        overview!.IsPaused.Should().BeFalse();

        using var afterScope = _fixture.CreateScope();
        var afterStorage = afterScope.ServiceProvider.GetRequiredService<JobStorage>();
        var afterEnqueued = afterStorage.GetMonitoringApi().EnqueuedCount("default");
        afterEnqueued.Should().BeGreaterThan(beforeEnqueued);
    }
}
