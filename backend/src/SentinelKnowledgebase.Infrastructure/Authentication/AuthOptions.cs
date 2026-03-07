namespace SentinelKnowledgebase.Infrastructure.Authentication;

public sealed class AuthOptions
{
    public const string SectionName = "Authentication";

    public string JwtSigningKey { get; set; } = "development-signing-key-change-me-development-signing-key";
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
    public int RefreshTokenLifetimeDays { get; set; } = 14;
    public int DeviceCodeLifetimeMinutes { get; set; } = 10;
    public int DevicePollingIntervalSeconds { get; set; } = 2;
    public string FrontendUrl { get; set; } = "http://localhost:4200";
    public string BootstrapAdminEmail { get; set; } = string.Empty;
    public string BootstrapAdminPassword { get; set; } = string.Empty;
    public string BootstrapAdminDisplayName { get; set; } = "Sentinel Admin";

    public void ApplyDefaults()
    {
        var defaults = new AuthOptions();

        if (string.IsNullOrWhiteSpace(JwtSigningKey))
        {
            JwtSigningKey = defaults.JwtSigningKey;
        }

        if (AccessTokenLifetimeMinutes <= 0)
        {
            AccessTokenLifetimeMinutes = defaults.AccessTokenLifetimeMinutes;
        }

        if (RefreshTokenLifetimeDays <= 0)
        {
            RefreshTokenLifetimeDays = defaults.RefreshTokenLifetimeDays;
        }

        if (DeviceCodeLifetimeMinutes <= 0)
        {
            DeviceCodeLifetimeMinutes = defaults.DeviceCodeLifetimeMinutes;
        }

        if (DevicePollingIntervalSeconds <= 0)
        {
            DevicePollingIntervalSeconds = defaults.DevicePollingIntervalSeconds;
        }

        if (string.IsNullOrWhiteSpace(FrontendUrl))
        {
            FrontendUrl = defaults.FrontendUrl;
        }
    }
}
