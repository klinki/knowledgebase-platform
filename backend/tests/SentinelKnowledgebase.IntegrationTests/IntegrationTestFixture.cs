using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace SentinelKnowledgebase.IntegrationTests;

public class IntegrationTestFixture : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    private WebApplicationFactory<Program> _factory = null!;
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

        _factory = CreateApplicationFactory();
        HttpClient = _factory.CreateClient();
        var scope = _factory.Services.CreateScope();
        DbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.EnsureCreatedAsync();
    }
    
    public WebApplicationFactory<Program> CreateApplicationFactory(string environment = "Testing")
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);

                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = _container.GetConnectionString(),
                        ["Hangfire:RetryAttempts"] = "3",
                        ["Hangfire:RetryDelaysInSeconds:0"] = "1",
                        ["Hangfire:RetryDelaysInSeconds:1"] = "1",
                        ["Hangfire:RetryDelaysInSeconds:2"] = "1"
                    });
                });

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
                        options.UseNpgsql(_container.GetConnectionString(), o => o.UseVector());
                    });
                });
            });
    }

    public async Task DisposeAsync()
    {
        HttpClient.Dispose();
        _factory.Dispose();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
}
