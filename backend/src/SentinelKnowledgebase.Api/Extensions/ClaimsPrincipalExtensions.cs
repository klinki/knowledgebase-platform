using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SentinelKnowledgebase.Api.Extensions;

internal static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId)
    {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue("sub");
        return Guid.TryParse(userIdValue, out userId);
    }
}
