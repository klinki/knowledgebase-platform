using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Application.DTOs.Integrations;
using SentinelKnowledgebase.Application.DTOs.Labels;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Application.Services.Telegram;

public sealed class TelegramIntegrationService : ITelegramIntegrationService
{
    private static readonly Regex UrlRegex = new(@"https?://\S+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly ApplicationDbContext _dbContext;
    private readonly ICaptureService _captureService;
    private readonly ICaptureProcessingAdminService _captureProcessingAdminService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TelegramIntegrationOptions _options;
    private readonly ILogger<TelegramIntegrationService> _logger;

    public TelegramIntegrationService(
        ApplicationDbContext dbContext,
        ICaptureService captureService,
        ICaptureProcessingAdminService captureProcessingAdminService,
        IBackgroundJobClient backgroundJobClient,
        IHttpClientFactory httpClientFactory,
        IOptions<TelegramIntegrationOptions> options,
        ILogger<TelegramIntegrationService> logger)
    {
        _dbContext = dbContext;
        _captureService = captureService;
        _captureProcessingAdminService = captureProcessingAdminService;
        _backgroundJobClient = backgroundJobClient;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TelegramLinkStatusDto> GetStatusAsync(Guid ownerUserId)
    {
        var link = await _dbContext.TelegramChatLinks
            .Where(item => item.OwnerUserId == ownerUserId && item.UnlinkedAt == null)
            .OrderByDescending(item => item.LinkedAt)
            .FirstOrDefaultAsync();
        var pendingCode = await _dbContext.TelegramLinkCodes
            .Where(item => item.OwnerUserId == ownerUserId && item.ConsumedAt == null && item.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync();

        return new TelegramLinkStatusDto
        {
            IsLinked = link != null,
            TelegramChatId = link?.TelegramChatId,
            ChatDisplayName = link?.ChatDisplayName,
            SenderDisplayName = link?.SenderDisplayName,
            LinkedAt = link?.LinkedAt,
            PendingCode = pendingCode == null ? null : new TelegramLinkCodeResponseDto
            {
                Code = pendingCode.Code,
                ExpiresAt = pendingCode.ExpiresAt
            }
        };
    }

    public async Task<TelegramLinkCodeResponseDto> IssueLinkCodeAsync(Guid ownerUserId)
    {
        var activeCode = await _dbContext.TelegramLinkCodes
            .Where(item => item.OwnerUserId == ownerUserId && item.ConsumedAt == null && item.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync();
        if (activeCode != null)
        {
            return new TelegramLinkCodeResponseDto { Code = activeCode.Code, ExpiresAt = activeCode.ExpiresAt };
        }

        var code = $"SNT-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.LinkCodeTtlMinutes);
        _dbContext.TelegramLinkCodes.Add(new TelegramLinkCode
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Code = code,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt
        });
        await _dbContext.SaveChangesAsync();

        return new TelegramLinkCodeResponseDto { Code = code, ExpiresAt = expiresAt };
    }

    public async Task UnlinkAsync(Guid ownerUserId)
    {
        var activeLinks = await _dbContext.TelegramChatLinks
            .Where(item => item.OwnerUserId == ownerUserId && item.UnlinkedAt == null)
            .ToListAsync();

        foreach (var link in activeLinks)
        {
            link.UnlinkedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task PollAndIngestAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            return;
        }

        var state = await _dbContext.TelegramIngestionStates.FirstOrDefaultAsync(item => item.Id == TelegramIngestionState.SingletonId, cancellationToken)
            ?? new TelegramIngestionState { Id = TelegramIngestionState.SingletonId, UpdatedAt = DateTimeOffset.UtcNow };

        if (_dbContext.Entry(state).State == EntityState.Detached)
        {
            _dbContext.TelegramIngestionStates.Add(state);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var endpoint = $"https://api.telegram.org/bot{_options.BotToken}/getUpdates?offset={state.LastProcessedUpdateId + 1}&timeout={_options.PollTimeoutSeconds}&limit={_options.PollLimit}";
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var updates = json.RootElement.GetProperty("result");
        long maxUpdateId = state.LastProcessedUpdateId;

        foreach (var update in updates.EnumerateArray())
        {
            var updateId = update.GetProperty("update_id").GetInt64();
            if (updateId > maxUpdateId)
            {
                maxUpdateId = updateId;
            }

            if (!update.TryGetProperty("message", out var message))
            {
                continue;
            }

            if (!message.TryGetProperty("chat", out var chat) || !chat.TryGetProperty("type", out var chatType) || chatType.GetString() != "private")
            {
                continue;
            }

            var text = message.TryGetProperty("text", out var textNode) ? textNode.GetString() : null;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var chatId = chat.GetProperty("id").GetInt64();
            var telegramUserId = message.TryGetProperty("from", out var fromNode) && fromNode.TryGetProperty("id", out var fromId)
                ? fromId.GetInt64()
                : 0;
            var messageId = message.GetProperty("message_id").GetInt64();

            if (await TryConsumeLinkCodeAsync(text.Trim(), chatId, telegramUserId, chat, fromNode: message.TryGetProperty("from", out var sender) ? sender : null, cancellationToken))
            {
                continue;
            }

            var link = await _dbContext.TelegramChatLinks
                .Where(item => item.TelegramChatId == chatId && item.UnlinkedAt == null)
                .OrderByDescending(item => item.LinkedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (link == null)
            {
                continue;
            }

            var url = UrlRegex.Match(text).Value;
            var metadata = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["source"] = "telegram",
                ["importSource"] = "telegram_bot",
                ["telegramChatId"] = chatId,
                ["telegramUserId"] = telegramUserId,
                ["telegramMessageId"] = messageId,
                ["telegramUpdateId"] = updateId,
                ["receivedAt"] = DateTimeOffset.UtcNow,
                ["chatDisplayName"] = link.ChatDisplayName,
                ["senderDisplayName"] = link.SenderDisplayName
            });

            var request = new CaptureRequestDto
            {
                ContentType = ContentType.Note,
                RawContent = text.Length > _options.MaxRawContentLength ? text[.._options.MaxRawContentLength] : text,
                SourceUrl = string.IsNullOrWhiteSpace(url) ? string.Empty : url,
                Metadata = metadata,
                Tags = ["telegram"],
                Labels = [new LabelAssignmentDto { Category = "Source", Value = "Telegram" }]
            };

            var capture = await _captureService.CreateCaptureAsync(link.OwnerUserId, request);
            if (!await _captureProcessingAdminService.IsPausedAsync())
            {
                _backgroundJobClient.Enqueue<ICaptureService>(service => service.ProcessCaptureAsync(capture.Id));
            }
        }

        if (maxUpdateId > state.LastProcessedUpdateId)
        {
            state.LastProcessedUpdateId = maxUpdateId;
            state.UpdatedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<bool> TryConsumeLinkCodeAsync(
        string incomingText,
        long chatId,
        long telegramUserId,
        JsonElement chat,
        JsonElement? fromNode,
        CancellationToken cancellationToken)
    {
        var code = incomingText.Trim().ToUpperInvariant();
        var linkCode = await _dbContext.TelegramLinkCodes
            .Where(item => item.Code == code && item.ConsumedAt == null && item.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (linkCode == null)
        {
            return false;
        }

        var existingLinks = await _dbContext.TelegramChatLinks
            .Where(item => item.OwnerUserId == linkCode.OwnerUserId && item.UnlinkedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var existingLink in existingLinks)
        {
            existingLink.UnlinkedAt = DateTimeOffset.UtcNow;
        }

        var chatTitle = chat.TryGetProperty("username", out var username)
            ? username.GetString()
            : chat.TryGetProperty("first_name", out var firstName) ? firstName.GetString() : null;
        var sender = fromNode?.TryGetProperty("username", out var senderUsername) == true
            ? senderUsername.GetString()
            : fromNode?.TryGetProperty("first_name", out var senderFirstName) == true ? senderFirstName.GetString() : null;

        _dbContext.TelegramChatLinks.Add(new TelegramChatLink
        {
            Id = Guid.NewGuid(),
            OwnerUserId = linkCode.OwnerUserId,
            TelegramChatId = chatId,
            TelegramUserId = telegramUserId,
            ChatDisplayName = chatTitle,
            SenderDisplayName = sender,
            LinkedAt = DateTimeOffset.UtcNow
        });

        linkCode.ConsumedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Linked telegram chat {ChatId} to user {OwnerUserId}", chatId, linkCode.OwnerUserId);

        return true;
    }
}
