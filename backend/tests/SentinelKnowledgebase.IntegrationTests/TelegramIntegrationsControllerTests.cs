using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using SentinelKnowledgebase.Application.DTOs.Integrations;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Data;

using Xunit;

namespace SentinelKnowledgebase.IntegrationTests;

[Collection("IntegrationTests")]
public class TelegramIntegrationsControllerTests
{
    private readonly IntegrationTestFixture _fixture;

    public TelegramIntegrationsControllerTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StatusAndLinkCodeEndpoints_ShouldReturnPendingCode()
    {
        var member = await _fixture.CreateMemberClientAsync();
        using var client = member.Client;

        var initialStatusResponse = await client.GetAsync("/api/v1/integrations/telegram/status");
        initialStatusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var initialStatus = await initialStatusResponse.Content.ReadFromJsonAsync<TelegramLinkStatusDto>();
        initialStatus.Should().NotBeNull();
        initialStatus!.IsLinked.Should().BeFalse();
        initialStatus.PendingCode.Should().BeNull();

        var codeResponse = await client.PostAsync("/api/v1/integrations/telegram/link-code", null);
        codeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var issuedCode = await codeResponse.Content.ReadFromJsonAsync<TelegramLinkCodeResponseDto>();
        issuedCode.Should().NotBeNull();
        issuedCode!.Code.Should().StartWith("SNT-");

        var statusAfterIssueResponse = await client.GetAsync("/api/v1/integrations/telegram/status");
        statusAfterIssueResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var statusAfterIssue = await statusAfterIssueResponse.Content.ReadFromJsonAsync<TelegramLinkStatusDto>();
        statusAfterIssue.Should().NotBeNull();
        statusAfterIssue!.PendingCode.Should().NotBeNull();
        statusAfterIssue.PendingCode!.Code.Should().Be(issuedCode.Code);
    }

    [Fact]
    public async Task Polling_ShouldConsumeLinkCode_AndCreateCaptureForLinkedChat()
    {
        var fakeTelegramApi = _fixture.GetFakeTelegramApi();
        fakeTelegramApi.Reset();

        var member = await _fixture.CreateMemberClientAsync();
        using var client = member.Client;
        var memberUserId = await _fixture.GetUserIdByEmailAsync(member.Email);

        var codeResponse = await client.PostAsync("/api/v1/integrations/telegram/link-code", null);
        codeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var issuedCode = await codeResponse.Content.ReadFromJsonAsync<TelegramLinkCodeResponseDto>();
        issuedCode.Should().NotBeNull();

        fakeTelegramApi.EnqueueGetUpdatesResponse($$"""
        {
          "ok": true,
          "result": [
            {
              "update_id": 1001,
              "message": {
                "message_id": 11,
                "text": "{{issuedCode!.Code}}",
                "chat": { "id": 555001, "type": "private", "username": "member_chat" },
                "from": { "id": 987001, "username": "member_sender" }
              }
            },
            {
              "update_id": 1002,
              "message": {
                "message_id": 12,
                "text": "hello from telegram https://example.com/telegram-link",
                "chat": { "id": 555001, "type": "private", "username": "member_chat" },
                "from": { "id": 987001, "username": "member_sender" }
              }
            }
          ]
        }
        """);

        using (var scope = _fixture.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ITelegramIntegrationService>();
            await service.PollAndIngestAsync(CancellationToken.None);
        }

        using var assertScope = _fixture.CreateScope();
        var dbContext = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var link = await dbContext.TelegramChatLinks
            .Where(item => item.OwnerUserId == memberUserId && item.UnlinkedAt == null)
            .SingleOrDefaultAsync();
        link.Should().NotBeNull();
        link!.TelegramChatId.Should().Be(555001);

        var capture = await dbContext.RawCaptures
            .Where(item => item.OwnerUserId == memberUserId)
            .SingleOrDefaultAsync();
        capture.Should().NotBeNull();
        capture!.SourceUrl.Should().Be("https://example.com/telegram-link");
        capture.RawContent.Should().Contain("hello from telegram");

        capture.Metadata.Should().NotBeNullOrWhiteSpace();
        using var metadata = JsonDocument.Parse(capture.Metadata!);
        metadata.RootElement.GetProperty("source").GetString().Should().Be("telegram");
        metadata.RootElement.GetProperty("importSource").GetString().Should().Be("telegram_bot");
        metadata.RootElement.GetProperty("telegramChatId").GetInt64().Should().Be(555001);
        metadata.RootElement.GetProperty("telegramUpdateId").GetInt64().Should().Be(1002);
    }

    [Fact]
    public async Task UnlinkEndpoint_ShouldDeactivateActiveLink()
    {
        var member = await _fixture.CreateMemberClientAsync();
        using var client = member.Client;
        var memberUserId = await _fixture.GetUserIdByEmailAsync(member.Email);

        await _fixture.ExecuteDbContextAsync(dbContext =>
        {
            dbContext.TelegramChatLinks.Add(new TelegramChatLink
            {
                Id = Guid.NewGuid(),
                OwnerUserId = memberUserId,
                TelegramChatId = 700100,
                TelegramUserId = 800100,
                ChatDisplayName = "to-unlink",
                SenderDisplayName = "sender",
                LinkedAt = DateTimeOffset.UtcNow
            });

            return Task.CompletedTask;
        });

        var unlinkResponse = await client.DeleteAsync("/api/v1/integrations/telegram/link");
        unlinkResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var activeLink = await dbContext.TelegramChatLinks
            .Where(item => item.OwnerUserId == memberUserId && item.UnlinkedAt == null)
            .SingleOrDefaultAsync();
        activeLink.Should().BeNull();
    }
}
