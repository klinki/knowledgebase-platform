using Microsoft.AspNetCore.Identity;

namespace SentinelKnowledgebase.Infrastructure.Authentication;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public string? DefaultLanguageCode { get; set; }
    public List<UserPreservedLanguage> PreservedLanguages { get; set; } = new();
}
