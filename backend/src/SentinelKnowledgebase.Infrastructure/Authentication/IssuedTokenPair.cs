namespace SentinelKnowledgebase.Infrastructure.Authentication;

public sealed class IssuedTokenPair
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}
