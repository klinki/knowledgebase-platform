using Microsoft.AspNetCore.Identity;

namespace SentinelKnowledgebase.Infrastructure.Authentication;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
}
