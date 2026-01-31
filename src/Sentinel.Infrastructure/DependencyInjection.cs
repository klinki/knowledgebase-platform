using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sentinel.Application.Interfaces;
using Sentinel.Infrastructure.Data;
using Sentinel.Infrastructure.Repositories;
using Sentinel.Infrastructure.Services;

namespace Sentinel.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SentinelDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'SentinelDb' is not configured.");
        }

        services.AddDbContext<SentinelDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));

        services.AddScoped<IRawCaptureRepository, RawCaptureRepository>();
        services.AddScoped<IProcessedInsightRepository, ProcessedInsightRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IInsightExtractionService, StubInsightExtractionService>();
        services.AddSingleton<IEmbeddingService, StubEmbeddingService>();

        return services;
    }
}
