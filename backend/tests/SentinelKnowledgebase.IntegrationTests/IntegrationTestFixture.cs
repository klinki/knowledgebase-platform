using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SentinelKnowledgebase.Application.DTOs.Auth;
using SentinelKnowledgebase.Infrastructure.Authentication;
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
    
    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder("pgvector/pgvector:pg18")
            .WithDatabase("sentinel_knowledgebase")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
        
        await _container.StartAsync();

        var dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_container.GetConnectionString(), options => options.UseVector())
            .Options;

        await using (var dbContext = new ApplicationDbContext(dbContextOptions))
        {
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
        }

        _factory = CreateApplicationFactory();
        using var scope = _factory.Services.CreateScope();
        var bootstrapper = scope.ServiceProvider.GetRequiredService<IdentityBootstrapper>();
        await bootstrapper.SeedAsync();

        HttpClient = _factory.CreateClient();
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

    public async Task<(HttpClient Client, string Email, string Password)> CreateMemberClientAsync(
        string? email = null,
        string? displayName = null,
        string password = "Member1234!")
    {
        email ??= $"member-{Guid.NewGuid():N}@sentinel.test";
        displayName ??= "Integration Member";

        using var adminClient = await CreateAuthenticatedClientAsync();
        var invitationResponse = await adminClient.PostAsJsonAsync("/api/auth/invitations", new InvitationRequestDto
        {
            Email = email,
            DisplayName = displayName,
            Role = AuthRoles.Member
        });

        invitationResponse.EnsureSuccessStatusCode();
        var invitation = await invitationResponse.Content.ReadFromJsonAsync<InvitationResponseDto>();
        if (invitation == null)
        {
            throw new InvalidOperationException("Expected invitation response payload.");
        }

        using var anonymousClient = CreateClient();
        var acceptResponse = await anonymousClient.PostAsJsonAsync("/api/auth/invitations/accept", new AcceptInvitationRequestDto
        {
            Token = invitation.Token,
            DisplayName = displayName,
            Password = password
        });

        acceptResponse.EnsureSuccessStatusCode();
        var memberClient = await CreateAuthenticatedClientAsync(email, password);
        return (memberClient, email, password);
    }

    public async Task<Guid> GetUserIdByEmailAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == email);
        return user.Id;
    }

    public async Task ExecuteDbContextAsync(Func<ApplicationDbContext, Task> action)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await action(dbContext);
        await dbContext.SaveChangesAsync();
    }

    public IServiceScope CreateScope()
    {
        return _factory.Services.CreateScope();
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
