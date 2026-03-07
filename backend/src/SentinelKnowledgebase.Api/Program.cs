using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.PostgreSql;
using OpenTelemetry.Metrics;
using Serilog;
using Scalar.AspNetCore;
using SentinelKnowledgebase.Api.HealthChecks;
using SentinelKnowledgebase.Application;
using SentinelKnowledgebase.Application.Services;
using SentinelKnowledgebase.Infrastructure.Authentication;
using SentinelKnowledgebase.Infrastructure.Data;
using SentinelKnowledgebase.Infrastructure;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer((schema, context, _) =>
    {
        var enumType = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
        if (!enumType.IsEnum)
        {
            return Task.CompletedTask;
        }

        schema.Type = Microsoft.OpenApi.JsonSchemaType.String;
        schema.Format = null;
        var enumValues = schema.Enum ?? new List<JsonNode>();
        enumValues.Clear();

        foreach (var enumName in Enum.GetNames(enumType))
        {
            enumValues.Add(JsonValue.Create(enumName)!);
        }

        schema.Enum = enumValues;

        return Task.CompletedTask;
    });
});
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
builder.Services.AddFluentValidationAutoValidation();
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("postgresql")
    .AddCheck<HangfireStorageHealthCheck>("hangfire_storage");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar/"));
}

using (var scope = app.Services.CreateScope())
{
    var bootstrapper = scope.ServiceProvider.GetRequiredService<IdentityBootstrapper>();
    await bootstrapper.SeedAsync();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorizationFilter(app.Environment)]
});

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/api/v1/health");

app.Run();

public partial class Program { }
