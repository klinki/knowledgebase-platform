using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Tests.Integration;

public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    public WebApplicationFactory<global::Program> Factory { get; private set; } = null!;

    public IntegrationTestFixture()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("pgvector/pgvector:pg16")
            .WithDatabase("sentinel_test")
            .WithUsername("test")
            .WithPassword("test123")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        Factory = new WebApplicationFactory<global::Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add DbContext with test container connection
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(_postgresContainer.GetConnectionString(), npgsqlOptions =>
                        {
                            npgsqlOptions.UseVector();
                        });
                    });

                    // Ensure database is created and migrations are applied
                    using var scope = services.BuildServiceProvider().CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    dbContext.Database.Migrate();
                });
            });
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }
}

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
}
