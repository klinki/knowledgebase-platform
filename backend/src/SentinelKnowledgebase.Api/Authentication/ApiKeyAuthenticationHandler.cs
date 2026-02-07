using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace SentinelKnowledgebase.Api.Authentication;

/// <summary>
/// API Key authentication handler for validating requests from the browser extension
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthenticationHandler> _logger;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
        _logger = logger.CreateLogger<ApiKeyAuthenticationHandler>();
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Skip authentication for health check endpoint
        if (Request.Path.StartsWithSegments("/api/v1/health"))
        {
            var claims = new[] { new Claim(ClaimTypes.Name, "health-check") };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        // Check for API key in Authorization header
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            _logger.LogWarning("Missing Authorization header");
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header"));
        }

        var authHeaderValue = authHeader.ToString();
        if (!authHeaderValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Invalid Authorization header format");
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header format. Expected 'Bearer <api-key>'"));
        }

        var providedApiKey = authHeaderValue.Substring("Bearer ".Length).Trim();
        var validApiKey = _configuration["ApiKey"];

        if (string.IsNullOrEmpty(validApiKey))
        {
            _logger.LogError("API key not configured in application settings");
            return Task.FromResult(AuthenticateResult.Fail("API key not configured on server"));
        }

        if (!string.Equals(providedApiKey, validApiKey, StringComparison.Ordinal))
        {
            _logger.LogWarning("Invalid API key provided");
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        // Create claims for authenticated user
        var authenticatedClaims = new[]
        {
            new Claim(ClaimTypes.Name, "api-user"),
            new Claim(ClaimTypes.Role, "User")
        };
        var authenticatedIdentity = new ClaimsIdentity(authenticatedClaims, Scheme.Name);
        var authenticatedPrincipal = new ClaimsPrincipal(authenticatedIdentity);
        var authenticatedTicket = new AuthenticationTicket(authenticatedPrincipal, Scheme.Name);

        _logger.LogDebug("API key authentication successful");
        return Task.FromResult(AuthenticateResult.Success(authenticatedTicket));
    }
}

/// <summary>
/// Options for API key authentication
/// </summary>
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public string Scheme => DefaultScheme;
}
