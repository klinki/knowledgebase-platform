namespace SentinelKnowledgebase.Infrastructure.Authentication;

public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid? DeviceAuthorizationId { get; set; }
    public DeviceAuthorization? DeviceAuthorization { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string TokenName { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public bool IsRevoked => RevokedAt.HasValue;
}
