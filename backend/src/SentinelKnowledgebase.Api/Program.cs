using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.PostgreSql;
using OpenTelemetry.Metrics;
using Serilog;
using SentinelKnowledgebase.Api.BackgroundProcessing;
using SentinelKnowledgebase.Api.HealthChecks;
using SentinelKnowledgebase.Application;
using SentinelKnowledgebase.Application.Services;
using SentinelKnowledgebase.Infrastructure.Data;
using SentinelKnowledgebase.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services
    .AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter(MonitoringService.MeterName)
            .AddOtlpExporter(options =>
            {
                var endpoint = builder.Configuration["OpenTelemetry:Otlp:Endpoint"];
                if (!string.IsNullOrWhiteSpace(endpoint))
                {
                    options.Endpoint = new Uri(endpoint);
                }
            });
    });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHangfireServer();
}
builder.Services.AddSingleton<ICaptureProcessingQueue, CaptureProcessingQueue>();
builder.Services.AddHostedService<CaptureProcessingBackgroundService>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("postgresql")
    .AddCheck<CaptureProcessingQueueHealthCheck>("capture_processing_queue");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHangfireDashboard("/hangfire");
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
