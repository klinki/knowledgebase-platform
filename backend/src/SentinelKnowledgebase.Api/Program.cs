using FluentValidation.AspNetCore;
using SentinelKnowledgebase.Api.Authentication;
using SentinelKnowledgebase.Api.Services;
using SentinelKnowledgebase.Application;
using SentinelKnowledgebase.Application.Services.Background;
using SentinelKnowledgebase.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add FluentValidation auto-validation
builder.Services.AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Program>());

// Add API Key authentication
builder.Services.AddAuthentication(ApiKeyAuthenticationOptions.DefaultScheme)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.DefaultScheme,
        null);

builder.Services.AddAuthorization();

// Add health checks
builder.Services.AddHealthChecks();

// Add background task queue and hosted service
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<CaptureProcessingHostedService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Map health check endpoint
app.MapHealthChecks("/api/v1/health");

app.MapControllers();

app.Run();

public partial class Program { }
