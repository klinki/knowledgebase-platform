using Hangfire;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SentinelKnowledgebase.Api.HealthChecks;

public class HangfireStorageHealthCheck : IHealthCheck
{
    private readonly JobStorage _jobStorage;

    public HangfireStorageHealthCheck(JobStorage jobStorage)
    {
        _jobStorage = jobStorage;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var servers = _jobStorage.GetMonitoringApi().Servers();
            return Task.FromResult(HealthCheckResult.Healthy($"Hangfire storage is reachable. Active servers: {servers.Count}."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Hangfire storage check failed.", ex));
        }
    }
}
