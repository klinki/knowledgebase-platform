namespace SentinelKnowledgebase.Infrastructure.Authentication;

public sealed class UserInvitation
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public Guid InvitedByUserId { get; set; }
    public ApplicationUser InvitedByUser { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
}
