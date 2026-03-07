using System.Security.Claims;

using Hangfire.Annotations;
using Hangfire.Dashboard;

using SentinelKnowledgebase.Infrastructure.Authentication;

namespace SentinelKnowledgebase.Api.HealthChecks;

public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly IWebHostEnvironment _environment;

    public HangfireDashboardAuthorizationFilter(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public bool Authorize([NotNull] DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (_environment.IsDevelopment() && httpContext.Request.IsLocalRequest())
        {
            return true;
        }

        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole(AuthRoles.Admin) &&
               httpContext.User.HasClaim(ClaimTypes.NameIdentifier, httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty);
    }
}
