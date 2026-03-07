using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SentinelKnowledgebase.Application.DTOs.Auth;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace SentinelKnowledgebase.IntegrationTests;

public class IntegrationTestFixture : IAsyncLifetime
{
    public const string BootstrapAdminEmail = "admin@sentinel.test";
    public const string BootstrapAdminPassword = "Password123!";

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
                        ["Hangfire:RetryDelaysInSeconds:2"] = "1",
                        ["Authentication:FrontendUrl"] = "http://localhost:4200",
                        ["Authentication:JwtSigningKey"] = "integration-tests-signing-key-integration-tests-signing-key",
                        ["Authentication:BootstrapAdminEmail"] = BootstrapAdminEmail,
                        ["Authentication:BootstrapAdminPassword"] = BootstrapAdminPassword,
                        ["Authentication:BootstrapAdminDisplayName"] = "Integration Admin"
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

    public HttpClient CreateClient(bool allowAutoRedirect = false)
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = allowAutoRedirect
        });
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email = BootstrapAdminEmail,
        string password = BootstrapAdminPassword)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = email,
            Password = password
        });

        response.EnsureSuccessStatusCode();
        return client;
    }

    public async Task DisposeAsync()
    {
        HttpClient?.Dispose();
        _factory?.Dispose();
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
}
