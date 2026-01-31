using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Sentinel.Api.IntegrationTests;

public sealed class ApiTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public ApiTestFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("pgvector/pgvector:pg16")
            .WithDatabase("sentinel")
            .WithUsername("sentinel")
            .WithPassword("sentinelpassword")
            .Build();
    }

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Factory = new SentinelApiFactory(_container.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        Factory.Dispose();
        await _container.DisposeAsync();
    }
}
