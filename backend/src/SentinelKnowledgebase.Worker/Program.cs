using Hangfire;
using Hangfire.PostgreSql;
using Serilog;
using SentinelKnowledgebase.Application;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Infrastructure;
using SentinelKnowledgebase.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilog((services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<CaptureProcessingStateFilter>();

var hangfireRetryAttempts = builder.Configuration.GetValue<int?>("Hangfire:RetryAttempts") ?? 10;
var hangfireRetryDelays = builder.Configuration.GetSection("Hangfire:RetryDelaysInSeconds").Get<int[]>();
var retryFilter = new AutomaticRetryAttribute
{
    Attempts = hangfireRetryAttempts
};
if (hangfireRetryDelays is { Length: > 0 })
{
    retryFilter.DelaysInSeconds = hangfireRetryDelays;
}
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseFilter(retryFilter)
    .UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));
builder.Services.AddHangfireServer();

var host = builder.Build();
GlobalJobFilters.Filters.Add(host.Services.GetRequiredService<CaptureProcessingStateFilter>());

var recurringJobManager = host.Services.GetRequiredService<IRecurringJobManager>();
recurringJobManager.AddOrUpdate<IInsightClusteringService>(
    "refresh-stale-insight-clusters",
    service => service.RebuildStaleOwnerClustersAsync(),
    Cron.Daily);
host.Run();
