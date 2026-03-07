namespace SentinelKnowledgebase.Infrastructure.Authentication;

public sealed class DeviceAuthorization
{
    public Guid Id { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public Guid? ApprovedByUserId { get; set; }
    public ApplicationUser? ApprovedByUser { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public bool Denied { get; set; }
}
