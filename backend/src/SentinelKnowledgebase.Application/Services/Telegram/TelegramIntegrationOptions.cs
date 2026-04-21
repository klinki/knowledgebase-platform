namespace SentinelKnowledgebase.Application.Services.Telegram;

public class TelegramIntegrationOptions
{
    public const string SectionName = "Telegram";

    public string BotToken { get; set; } = string.Empty;
    public int PollTimeoutSeconds { get; set; } = 20;
    public int PollLimit { get; set; } = 25;
    public int PollCadenceSeconds { get; set; } = 3;
    public int LinkCodeTtlMinutes { get; set; } = 10;
    public int MaxRawContentLength { get; set; } = 8000;
}
