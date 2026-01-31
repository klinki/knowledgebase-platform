using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SentinelKnowledgebase.Infrastructure.Data;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    npgsqlOptions.UseVector();
                });
        });

        services.AddScoped<IRawCaptureRepository, RawCaptureRepository>();
        services.AddScoped<IProcessedInsightRepository, ProcessedInsightRepository>();
        services.AddScoped<ITagRepository, TagRepository>();

        return services;
    }
}
