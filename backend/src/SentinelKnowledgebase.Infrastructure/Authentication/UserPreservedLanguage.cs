namespace SentinelKnowledgebase.Infrastructure.Authentication;

public sealed class UserPreservedLanguage
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string LanguageCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
