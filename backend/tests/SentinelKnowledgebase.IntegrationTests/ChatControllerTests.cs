using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pgvector;
using SentinelKnowledgebase.Application.DTOs.Assistant;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using Xunit;

namespace SentinelKnowledgebase.IntegrationTests;

[Collection("IntegrationTests")]
public class ChatControllerTests
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IntegrationTestFixture _fixture;

    public ChatControllerTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetSession_ShouldCreateSessionForFirstTimeUser_WithoutServerError()
    {
        var member = await _fixture.CreateMemberClientAsync();
        using var memberClient = member.Client;
        var memberUserId = await _fixture.GetUserIdByEmailAsync(member.Email);

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.AssistantChatSessions
                .Count(session => session.OwnerUserId == memberUserId)
                .Should()
                .Be(0);
            await Task.CompletedTask;
        });

        var response = await memberClient.GetAsync("/api/v1/chat/session");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<AssistantChatSessionDto>(ResponseJsonOptions);
        payload.Should().NotBeNull();
        payload!.Id.Should().NotBe(Guid.Empty);

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            var sessions = dbContext.AssistantChatSessions
                .Where(session => session.OwnerUserId == memberUserId)
                .ToList();
            sessions.Should().HaveCount(1);
            sessions[0].Id.Should().Be(payload.Id);
            await Task.CompletedTask;
        });
    }

    [Fact]
    public async Task ChatFlow_ShouldFindDeletedTweets_RequireConfirmation_AndEnforceOwnerScope()
    {
        using var ownerClient = await _fixture.CreateAuthenticatedClientAsync();
        var foreign = await _fixture.CreateMemberClientAsync();
        using var foreignClient = foreign.Client;

        var ownerUserId = await _fixture.GetUserIdByEmailAsync(IntegrationTestFixture.BootstrapAdminEmail);
        var foreignUserId = await _fixture.GetUserIdByEmailAsync(foreign.Email);
        var ownerDeletedA = Guid.NewGuid();
        var ownerDeletedB = Guid.NewGuid();
        var ownerIgnored = Guid.NewGuid();
        var foreignDeleted = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.RawCaptures.AddRange(
                new RawCapture
                {
                    Id = ownerDeletedA,
                    OwnerUserId = ownerUserId,
                    SourceUrl = "https://twitter.com/a",
                    ContentType = ContentType.Tweet,
                    RawContent = "tweet-a",
                    Status = CaptureStatus.Completed,
                    Metadata = """{"processingSkipCode":"twitter_suspended_account","processingSkipReason":"Suspended"}""",
                    CreatedAt = now.AddMinutes(-4),
                    ProcessedAt = now.AddMinutes(-3)
                },
                new RawCapture
                {
                    Id = ownerDeletedB,
                    OwnerUserId = ownerUserId,
                    SourceUrl = "https://twitter.com/b",
                    ContentType = ContentType.Tweet,
                    RawContent = "tweet-b",
                    Status = CaptureStatus.Completed,
                    Metadata = """{"processingSkipCode":"twitter_post_unavailable","processingSkipReason":"Unavailable"}""",
                    CreatedAt = now.AddMinutes(-2),
                    ProcessedAt = now.AddMinutes(-1)
                },
                new RawCapture
                {
                    Id = ownerIgnored,
                    OwnerUserId = ownerUserId,
                    SourceUrl = "https://twitter.com/c",
                    ContentType = ContentType.Tweet,
                    RawContent = "tweet-c",
                    Status = CaptureStatus.Completed,
                    Metadata = """{"processingSkipCode":"other_skip","processingSkipReason":"Other"}""",
                    CreatedAt = now
                },
                new RawCapture
                {
                    Id = foreignDeleted,
                    OwnerUserId = foreignUserId,
                    SourceUrl = "https://twitter.com/foreign",
                    ContentType = ContentType.Tweet,
                    RawContent = "tweet-foreign",
                    Status = CaptureStatus.Completed,
                    Metadata = """{"processingSkipCode":"twitter_account_limited","processingSkipReason":"Limited"}""",
                    CreatedAt = now
                });

            await Task.CompletedTask;
        });

        var searchResponse = await ownerClient.PostAsJsonAsync(
            "/api/v1/chat/session/messages",
            new AssistantChatMessageSendRequestDto { Message = "Find me all tweets from deleted accounts" });
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchPayload = await searchResponse.Content.ReadFromJsonAsync<AssistantChatMessageSendResponseDto>(ResponseJsonOptions);
        searchPayload.Should().NotBeNull();
        searchPayload!.AssistantMessage.ResultSet.Should().NotBeNull();
        searchPayload.AssistantMessage.ResultSet!.TotalCount.Should().Be(2);

        var deleteProposalResponse = await ownerClient.PostAsJsonAsync(
            "/api/v1/chat/session/messages",
            new AssistantChatMessageSendRequestDto { Message = "Now delete all these tweets" });
        deleteProposalResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteProposal = await deleteProposalResponse.Content.ReadFromJsonAsync<AssistantChatMessageSendResponseDto>(ResponseJsonOptions);
        deleteProposal.Should().NotBeNull();
        deleteProposal!.AssistantMessage.Action.Should().NotBeNull();
        deleteProposal.AssistantMessage.Action!.Status.Should().Be(AssistantChatActionStatus.PendingConfirmation);
        deleteProposal.AssistantMessage.Action.CaptureCount.Should().Be(2);

        var actionId = deleteProposal.AssistantMessage.Action.Id;
        var foreignConfirm = await foreignClient.PostAsync($"/api/v1/chat/actions/{actionId}/confirm", null);
        foreignConfirm.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var confirmResponse = await ownerClient.PostAsync($"/api/v1/chat/actions/{actionId}/confirm", null);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirmPayload = await confirmResponse.Content.ReadFromJsonAsync<AssistantChatActionResponseDto>(ResponseJsonOptions);
        confirmPayload.Should().NotBeNull();
        confirmPayload!.Action.Status.Should().Be(AssistantChatActionStatus.Executed);
        confirmPayload.Action.ExecutedCount.Should().Be(2);

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            var ownerRemaining = dbContext.RawCaptures
                .Where(capture => capture.OwnerUserId == ownerUserId)
                .Select(capture => capture.Id)
                .ToHashSet();
            ownerRemaining.Should().Contain(ownerIgnored);
            ownerRemaining.Should().NotContain(ownerDeletedA);
            ownerRemaining.Should().NotContain(ownerDeletedB);

            var foreignRemaining = dbContext.RawCaptures
                .Where(capture => capture.OwnerUserId == foreignUserId)
                .Select(capture => capture.Id)
                .ToHashSet();
            foreignRemaining.Should().Contain(foreignDeleted);

            await Task.CompletedTask;
        });
    }

    [Fact]
    public async Task ChatFlow_ShouldSearchCapturesWithHybridMatching_AndKeepDeleteConfirmationSafety()
    {
        using var ownerClient = await _fixture.CreateAuthenticatedClientAsync();

        var ownerUserId = await _fixture.GetUserIdByEmailAsync(IntegrationTestFixture.BootstrapAdminEmail);
        var semanticMatchCaptureId = Guid.NewGuid();
        var textMatchCaptureId = Guid.NewGuid();
        var ignoredCaptureId = Guid.NewGuid();
        var processedInsightId = Guid.NewGuid();
        var embeddingId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        const string query = "outage investigation";

        float[] embedding;
        using (var scope = _fixture.CreateScope())
        {
            var contentProcessor = scope.ServiceProvider.GetRequiredService<IContentProcessor>();
            embedding = await contentProcessor.GenerateEmbeddingAsync(query);
        }

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.RawCaptures.AddRange(
                new RawCapture
                {
                    Id = semanticMatchCaptureId,
                    OwnerUserId = ownerUserId,
                    SourceUrl = "https://example.com/semantic",
                    ContentType = ContentType.Article,
                    RawContent = "Unrelated body text",
                    Status = CaptureStatus.Completed,
                    CreatedAt = now.AddMinutes(-5),
                    ProcessedAt = now.AddMinutes(-4)
                },
                new RawCapture
                {
                    Id = textMatchCaptureId,
                    OwnerUserId = ownerUserId,
                    SourceUrl = "https://example.com/text",
                    ContentType = ContentType.Tweet,
                    RawContent = "The outage investigation timeline is attached.",
                    Status = CaptureStatus.Failed,
                    CreatedAt = now.AddMinutes(-3)
                },
                new RawCapture
                {
                    Id = ignoredCaptureId,
                    OwnerUserId = ownerUserId,
                    SourceUrl = "https://example.com/ignored",
                    ContentType = ContentType.Note,
                    RawContent = "This should not match the query.",
                    Status = CaptureStatus.Pending,
                    CreatedAt = now.AddMinutes(-1)
                });

            dbContext.ProcessedInsights.Add(new ProcessedInsight
            {
                Id = processedInsightId,
                OwnerUserId = ownerUserId,
                RawCaptureId = semanticMatchCaptureId,
                Title = "Outage investigation summary",
                Summary = "Root cause analysis in progress.",
                ProcessedAt = now.AddMinutes(-4)
            });

            dbContext.EmbeddingVectors.Add(new EmbeddingVector
            {
                Id = embeddingId,
                ProcessedInsightId = processedInsightId,
                Vector = new Vector(embedding),
                CreatedAt = now.AddMinutes(-4)
            });

            await Task.CompletedTask;
        });

        var searchResponse = await ownerClient.PostAsJsonAsync(
            "/api/v1/chat/session/messages",
            new AssistantChatMessageSendRequestDto { Message = $"Search captures for {query}" });
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchPayload = await searchResponse.Content.ReadFromJsonAsync<AssistantChatMessageSendResponseDto>(ResponseJsonOptions);
        searchPayload.Should().NotBeNull();
        searchPayload!.AssistantMessage.ResultSet.Should().NotBeNull();
        searchPayload.AssistantMessage.ResultSet!.QueryType.Should().Be("search_captures");
        searchPayload.AssistantMessage.ResultSet.TotalCount.Should().Be(2);

        var previewIds = searchPayload.AssistantMessage.ResultSet.PreviewItems
            .Select(item => item.CaptureId)
            .ToHashSet();
        previewIds.Should().Contain(semanticMatchCaptureId);
        previewIds.Should().Contain(textMatchCaptureId);

        var deleteProposalResponse = await ownerClient.PostAsJsonAsync(
            "/api/v1/chat/session/messages",
            new AssistantChatMessageSendRequestDto { Message = "Now delete all these tweets" });
        deleteProposalResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteProposal = await deleteProposalResponse.Content.ReadFromJsonAsync<AssistantChatMessageSendResponseDto>(ResponseJsonOptions);
        deleteProposal.Should().NotBeNull();
        deleteProposal!.AssistantMessage.Action.Should().NotBeNull();
        deleteProposal.AssistantMessage.Action!.Status.Should().Be(AssistantChatActionStatus.PendingConfirmation);
        deleteProposal.AssistantMessage.Action.CaptureCount.Should().Be(2);

        var confirmResponse = await ownerClient.PostAsync(
            $"/api/v1/chat/actions/{deleteProposal.AssistantMessage.Action.Id}/confirm",
            null);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            var remainingCaptureIds = dbContext.RawCaptures
                .Where(capture => capture.OwnerUserId == ownerUserId)
                .Select(capture => capture.Id)
                .ToHashSet();

            remainingCaptureIds.Should().Contain(ignoredCaptureId);
            remainingCaptureIds.Should().NotContain(semanticMatchCaptureId);
            remainingCaptureIds.Should().NotContain(textMatchCaptureId);

            await Task.CompletedTask;
        });
    }
}
