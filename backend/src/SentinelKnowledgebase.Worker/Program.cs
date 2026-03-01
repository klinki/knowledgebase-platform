using Hangfire;
using Hangfire.PostgreSql;
using Serilog;
using SentinelKnowledgebase.Application;
using SentinelKnowledgebase.Infrastructure;

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

var hangfireRetryAttempts = builder.Configuration.GetValue<int?>("Hangfire:RetryAttempts") ?? 3;
var hangfireRetryDelays = builder.Configuration.GetSection("Hangfire:RetryDelaysInSeconds").Get<int[]>() ?? [5, 15, 30];
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseFilter(new AutomaticRetryAttribute
    {
        Attempts = hangfireRetryAttempts,
        DelaysInSeconds = hangfireRetryDelays
    })
    .UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));
builder.Services.AddHangfireServer();

var host = builder.Build();
host.Run();
