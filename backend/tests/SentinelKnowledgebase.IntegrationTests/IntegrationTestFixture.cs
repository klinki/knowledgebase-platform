using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace SentinelKnowledgebase.IntegrationTests;

public class IntegrationTestFixture : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    public HttpClient HttpClient = null!;
    public ApplicationDbContext DbContext = null!;
    
    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder("pgvector/pgvector:pg18")
            .WithDatabase("sentinel_knowledgebase")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
        
        await _container.StartAsync();
        
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }
                    
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(_container.GetConnectionString());
                    });
                });
            });
        
        HttpClient = factory.CreateClient();
        var scope = factory.Services.CreateScope();
        DbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await DbContext.Database.EnsureCreatedAsync();
    }
    
    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
}
