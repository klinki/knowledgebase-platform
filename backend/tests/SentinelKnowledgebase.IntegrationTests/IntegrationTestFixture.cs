using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Concurrent;
using SentinelKnowledgebase.Application.DTOs.Auth;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Infrastructure.Authentication;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
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
                        ["Authentication:BootstrapAdminDisplayName"] = "Integration Admin",
                        ["Telegram:BotToken"] = "test-bot-token",
                        ["Telegram:PollTimeoutSeconds"] = "1",
                        ["Telegram:PollLimit"] = "25",
                        ["Telegram:PollCadenceSeconds"] = "1",
                        ["Telegram:LinkCodeTtlMinutes"] = "10",
                        ["Telegram:MaxRawContentLength"] = "8000"
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

                    services.AddScoped<IContentProcessor, FakeContentProcessor>();
                    services.RemoveAll<IHttpClientFactory>();
                    services.AddSingleton<FakeTelegramApiHttpClientFactory>();
                    services.AddSingleton<IHttpClientFactory>(sp => sp.GetRequiredService<FakeTelegramApiHttpClientFactory>());
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

    public FakeTelegramApiHttpClientFactory GetFakeTelegramApi()
    {
        return _factory.Services.GetRequiredService<FakeTelegramApiHttpClientFactory>();
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

internal sealed class FakeContentProcessor : IContentProcessor
{
    private static readonly ConcurrentQueue<string> EmbeddingInputs = new();

    public static void ResetObservations()
    {
        while (EmbeddingInputs.TryDequeue(out _))
        {
        }
    }

    public static IReadOnlyList<string> GetEmbeddingInputs()
    {
        return EmbeddingInputs.ToArray();
    }

    public string DenoiseContent(string content)
    {
        return content.Trim();
    }

    public Task<ContentInsights> ExtractInsightsAsync(
        string content,
        ContentType contentType,
        string? outputLanguageCode = null)
    {
        var languageMarker = string.IsNullOrWhiteSpace(outputLanguageCode) ? "source" : outputLanguageCode;
        return Task.FromResult(new ContentInsights
        {
            Title = $"{contentType} insight [{languageMarker}]",
            Summary = $"[{languageMarker}] {content}",
            KeyInsights = [$"Insight [{languageMarker}]"],
            ActionItems = [$"Action [{languageMarker}]"],
            SourceTitle = $"Original title for {contentType}",
            Author = "Original author"
        });
    }

    public Task<float[]> GenerateEmbeddingAsync(string text)
    {
        EmbeddingInputs.Enqueue(text);
        var seed = text
            .Aggregate(17, (current, character) => unchecked(current * 31 + character));
        var random = new Random(seed);
        var vector = new float[1536];

        for (var index = 0; index < vector.Length; index++)
        {
            vector[index] = (float)random.NextDouble() * 2 - 1;
        }

        var norm = (float)Math.Sqrt(vector.Sum(value => value * value));
        if (norm > 0)
        {
            for (var index = 0; index < vector.Length; index++)
            {
                vector[index] /= norm;
            }
        }

        return Task.FromResult(vector);
    }

    public Task<ClusterMetadata> GenerateClusterMetadataAsync(IReadOnlyCollection<string> summaries)
    {
        var firstSummary = summaries.FirstOrDefault()?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Topic";
        return Task.FromResult(new ClusterMetadata
        {
            Title = $"{firstSummary} Topic",
            Description = $"Cluster generated for {summaries.Count} related summaries.",
            Keywords = summaries
                .SelectMany(summary => summary.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Select(word => word.Trim().Trim(',', '.', ';', ':').ToLowerInvariant())
                .Where(word => word.Length >= 4)
                .Distinct()
                .Take(3)
                .ToList()
        });
    }
}


internal sealed class FakeTelegramApiHttpClientFactory : IHttpClientFactory
{
    private readonly ConcurrentQueue<string> _responses = new();

    public void Reset()
    {
        while (_responses.TryDequeue(out _))
        {
        }
    }

    public void EnqueueGetUpdatesResponse(string jsonPayload)
    {
        _responses.Enqueue(jsonPayload);
    }

    public HttpClient CreateClient(string name)
    {
        return new HttpClient(new QueueMessageHandler(_responses));
    }

    private sealed class QueueMessageHandler : HttpMessageHandler
    {
        private readonly ConcurrentQueue<string> _responses;

        public QueueMessageHandler(ConcurrentQueue<string> responses)
        {
            _responses = responses;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!_responses.TryDequeue(out var payload))
            {
                payload = "{\"ok\":true,\"result\":[]}";
            }

            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(payload)
            };

            return Task.FromResult(response);
        }
    }
}
