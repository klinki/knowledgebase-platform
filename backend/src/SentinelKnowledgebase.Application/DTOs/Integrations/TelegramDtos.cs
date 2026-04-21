namespace SentinelKnowledgebase.Application.DTOs.Integrations;

public sealed class TelegramLinkCodeResponseDto
{
    public string Code { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class TelegramLinkStatusDto
{
    public bool IsLinked { get; set; }
    public long? TelegramChatId { get; set; }
    public string? ChatDisplayName { get; set; }
    public string? SenderDisplayName { get; set; }
    public DateTimeOffset? LinkedAt { get; set; }
    public TelegramLinkCodeResponseDto? PendingCode { get; set; }
}
