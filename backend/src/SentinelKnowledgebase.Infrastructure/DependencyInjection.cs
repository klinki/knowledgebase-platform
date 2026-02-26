using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;
using SentinelKnowledgebase.Infrastructure.Data;
using SentinelKnowledgebase.Infrastructure.Repositories;

namespace SentinelKnowledgebase.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.UseVector()));
        
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IRawCaptureRepository, RawCaptureRepository>();
        services.AddScoped<IProcessedInsightRepository, ProcessedInsightRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IEmbeddingVectorRepository, EmbeddingVectorRepository>();
        
        return services;
    }
}
