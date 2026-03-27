using AwesomeAssertions;
using SentinelKnowledgebase.Application.DTOs.Auth;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.ImportCLI;
using System.Net;
using System.Text.Json;
using Xunit;

namespace SentinelKnowledgebase.UnitTests;

public class ImportCliHttpFlowTests
{
    [Fact]
    public async Task DeviceAuthClient_GetAccessTokenAsync_FirstRunShouldUseDeviceFlowAndCacheTokens()
    {
        using var temp = new TempDirectoryScope();
        var handler = new DelegateHttpMessageHandler((request, log) =>
        {
            return log.Path switch
            {
                "/api/auth/device/start" => ImportCliTestData.JsonResponse(HttpStatusCode.OK, new DeviceStartResponseDto
                {
                    DeviceCode = "device-code",
                    UserCode = "ABCD-1234",
                    VerificationUrl = "https://app.example/login?userCode=ABCD-1234",
                    ExpiresAt = DateTimeOffset.Parse("2026-03-27T11:00:00Z"),
                    IntervalSeconds = 0
                }),
                "/api/auth/device/poll" => ImportCliTestData.JsonResponse(HttpStatusCode.OK, new TokenResponseDto
                {
                    AccessToken = "access-token",
                    RefreshToken = "refresh-token",
                    ExpiresAt = DateTimeOffset.Parse("2026-03-27T12:00:00Z"),
                    User = new AuthUserDto
                    {
                        Id = Guid.NewGuid(),
                        Email = "member@example.com",
                        DisplayName = "Member",
                        Role = "member"
                    }
                }),
                _ => throw new InvalidOperationException($"Unexpected request {log.Path}")
            };
        });
        using var httpClient = new HttpClient(handler);
        var cache = new FileTokenCache(ImportCliTestData.JsonOptions, System.IO.Path.Combine(temp.Path, "auth-cache.json"));
        var reporter = new TestImportReporter();
        var authClient = new DeviceAuthClient(
            httpClient,
            cache,
            reporter,
            ImportCliTestData.JsonOptions,
            new StubTimeProvider(DateTimeOffset.Parse("2026-03-27T10:00:00Z")));

        var token = await authClient.GetAccessTokenAsync("https://sentinel.example", CancellationToken.None);
        var cachedSession = await cache.GetAsync("https://sentinel.example", CancellationToken.None);

        token.Should().Be("access-token");
        cachedSession.Should().NotBeNull();
        cachedSession!.RefreshToken.Should().Be("refresh-token");
        handler.Logs.Should().HaveCount(2);
        reporter.InfoMessages.Should().Contain(message => message.Contains("Starting Sentinel device sign-in"));
    }

    [Fact]
    public async Task DeviceAuthClient_GetAccessTokenAsync_WithExpiredTokenShouldRefresh()
    {
        using var temp = new TempDirectoryScope();
        var cache = new FileTokenCache(ImportCliTestData.JsonOptions, System.IO.Path.Combine(temp.Path, "auth-cache.json"));
        await cache.SaveAsync(new CachedAuthSession
        {
            ApiUrl = "https://sentinel.example",
            AccessToken = "old-access-token",
            RefreshToken = "refresh-token",
            ExpiresAt = DateTimeOffset.Parse("2026-03-27T10:00:10Z"),
            User = new AuthUserDto
            {
                Id = Guid.NewGuid(),
                Email = "member@example.com",
                DisplayName = "Member",
                Role = "member"
            }
        }, CancellationToken.None);

        var handler = new DelegateHttpMessageHandler((request, log) =>
        {
            return log.Path switch
            {
                "/api/auth/token/refresh" => ImportCliTestData.JsonResponse(HttpStatusCode.OK, new TokenResponseDto
                {
                    AccessToken = "new-access-token",
                    RefreshToken = "new-refresh-token",
                    ExpiresAt = DateTimeOffset.Parse("2026-03-27T12:00:00Z"),
                    User = new AuthUserDto
                    {
                        Id = Guid.NewGuid(),
                        Email = "member@example.com",
                        DisplayName = "Member",
                        Role = "member"
                    }
                }),
                _ => throw new InvalidOperationException($"Unexpected request {log.Path}")
            };
        });
        using var httpClient = new HttpClient(handler);
        var authClient = new DeviceAuthClient(
            httpClient,
            cache,
            new TestImportReporter(),
            ImportCliTestData.JsonOptions,
            new StubTimeProvider(DateTimeOffset.Parse("2026-03-27T10:00:45Z")));

        var token = await authClient.GetAccessTokenAsync("https://sentinel.example", CancellationToken.None);

        token.Should().Be("new-access-token");
        handler.Logs.Should().ContainSingle(log => log.Path == "/api/auth/token/refresh");
    }

    [Fact]
    public async Task SentinelCaptureClient_GetExistingTweetIdsAsync_ShouldIgnoreNonTwitterCaptures()
    {
        using var temp = new TempDirectoryScope();
        var cache = new FileTokenCache(ImportCliTestData.JsonOptions, System.IO.Path.Combine(temp.Path, "auth-cache.json"));
        await cache.SaveAsync(new CachedAuthSession
        {
            ApiUrl = "https://sentinel.example",
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            ExpiresAt = DateTimeOffset.Parse("2026-03-27T12:00:00Z"),
            User = new AuthUserDto()
        }, CancellationToken.None);

        var handler = new DelegateHttpMessageHandler((request, log) =>
        {
            return log.Path switch
            {
                "/api/v1/capture" => ImportCliTestData.JsonResponse(HttpStatusCode.OK, new List<CaptureResponseDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ContentType = ContentType.Tweet,
                        CreatedAt = DateTime.UtcNow,
                        Metadata = """{"source":"twitter","tweetId":"1"}""",
                        RawContent = "One",
                        SourceUrl = "https://twitter.com/i/web/status/1",
                        Status = CaptureStatus.Pending
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ContentType = ContentType.Article,
                        CreatedAt = DateTime.UtcNow,
                        Metadata = """{"source":"webpage","tweetId":"2"}""",
                        RawContent = "Two",
                        SourceUrl = "https://example.com",
                        Status = CaptureStatus.Pending
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ContentType = ContentType.Tweet,
                        CreatedAt = DateTime.UtcNow,
                        Metadata = """{"source":"twitter","tweet_id":"3"}""",
                        RawContent = "Three",
                        SourceUrl = "https://twitter.com/i/web/status/3",
                        Status = CaptureStatus.Pending
                    }
                }),
                _ => throw new InvalidOperationException($"Unexpected request {log.Path}")
            };
        });
        using var httpClient = new HttpClient(handler);
        var authClient = new DeviceAuthClient(
            httpClient,
            cache,
            new TestImportReporter(),
            ImportCliTestData.JsonOptions,
            new StubTimeProvider(DateTimeOffset.Parse("2026-03-27T10:00:00Z")));
        var captureClient = new SentinelCaptureClient(httpClient, authClient, ImportCliTestData.JsonOptions);

        var tweetIds = await captureClient.GetExistingTweetIdsAsync("https://sentinel.example", CancellationToken.None);

        tweetIds.Should().BeEquivalentTo(["1", "3"]);
    }

    [Fact]
    public async Task CliApplication_InvokeAsync_FirstRunShouldAuthenticateAndSubmitCapture()
    {
        using var temp = new TempDirectoryScope();
        ImportCliTestData.CreateArchiveDirectory(temp.Path, ImportCliTestData.SingleLikePayload(fullText: "CLI tweet"));
        var cache = new FileTokenCache(ImportCliTestData.JsonOptions, System.IO.Path.Combine(temp.Path, "auth-cache.json"));

        var handler = new DelegateHttpMessageHandler((request, log) =>
        {
            return log.Path switch
            {
                "/api/auth/device/start" => ImportCliTestData.JsonResponse(HttpStatusCode.OK, new DeviceStartResponseDto
                {
                    DeviceCode = "device-code",
                    UserCode = "ABCD-1234",
                    VerificationUrl = "https://app.example/login?userCode=ABCD-1234",
                    ExpiresAt = DateTimeOffset.Parse("2026-03-27T11:00:00Z"),
                    IntervalSeconds = 0
                }),
                "/api/auth/device/poll" => ImportCliTestData.JsonResponse(HttpStatusCode.OK, new TokenResponseDto
                {
                    AccessToken = "access-token",
                    RefreshToken = "refresh-token",
                    ExpiresAt = DateTimeOffset.Parse("2026-03-27T12:00:00Z"),
                    User = new AuthUserDto
                    {
                        Id = Guid.NewGuid(),
                        Email = "member@example.com",
                        DisplayName = "Member",
                        Role = "member"
                    }
                }),
                "/api/v1/capture" when log.Method == "GET" => ImportCliTestData.JsonResponse(HttpStatusCode.OK, new List<CaptureResponseDto>()),
                "/api/v1/capture" when log.Method == "POST" => ImportCliTestData.JsonResponse(HttpStatusCode.Accepted, new CaptureAcceptedDto
                {
                    Id = Guid.NewGuid(),
                    Message = "accepted"
                }),
                _ => throw new InvalidOperationException($"Unexpected request {log.Method} {log.Path}")
            };
        });

        using var httpClient = new HttpClient(handler);
        var reporter = new ConsoleImportReporter(new StringWriter(), new StringWriter());
        var authClient = new DeviceAuthClient(httpClient, cache, reporter, ImportCliTestData.JsonOptions, new StubTimeProvider(DateTimeOffset.Parse("2026-03-27T10:00:00Z")));
        var captureClient = new SentinelCaptureClient(httpClient, authClient, ImportCliTestData.JsonOptions);
        var service = new TwitterLikesImportService(
            new ArchiveInputResolver(),
            new TwitterLikesImportSource(),
            new TwitterLikeCaptureMapper(ImportCliTestData.JsonOptions),
            captureClient,
            reporter,
            new StubTimeProvider(DateTimeOffset.Parse("2026-03-27T10:00:00Z")));

        var output = new StringWriter();
        var error = new StringWriter();
        var cli = new CliApplication(service, output, error);

        var exitCode = await cli.InvokeAsync(["twitter", "likes", "--input", temp.Path, "--api-url", "https://sentinel.example"]);

        exitCode.Should().Be(0);
        handler.Logs.Should().ContainSingle(log => log.Method == "POST" && log.Path == "/api/v1/capture");
        output.ToString().Should().Contain("Successfully submitted captures: 1");
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task CliApplication_InvokeAsync_SecondRunShouldSkipDuplicateCapture()
    {
        using var temp = new TempDirectoryScope();
        ImportCliTestData.CreateArchiveDirectory(temp.Path, ImportCliTestData.SingleLikePayload());
        var cache = new FileTokenCache(ImportCliTestData.JsonOptions, System.IO.Path.Combine(temp.Path, "auth-cache.json"));
        await cache.SaveAsync(new CachedAuthSession
        {
            ApiUrl = "https://sentinel.example",
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            ExpiresAt = DateTimeOffset.Parse("2026-03-27T12:00:00Z"),
            User = new AuthUserDto()
        }, CancellationToken.None);

        var handler = new DelegateHttpMessageHandler((request, log) =>
        {
            return log.Path switch
            {
                "/api/v1/capture" when log.Method == "GET" => ImportCliTestData.JsonResponse(HttpStatusCode.OK, new List<CaptureResponseDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ContentType = ContentType.Tweet,
                        CreatedAt = DateTime.UtcNow,
                        Metadata = """{"source":"twitter","tweetId":"2018256260119101805"}""",
                        RawContent = "Existing capture",
                        SourceUrl = "https://twitter.com/i/web/status/2018256260119101805",
                        Status = CaptureStatus.Pending
                    }
                }),
                _ => throw new InvalidOperationException($"Unexpected request {log.Method} {log.Path}")
            };
        });

        using var httpClient = new HttpClient(handler);
        var reporter = new ConsoleImportReporter(new StringWriter(), new StringWriter());
        var authClient = new DeviceAuthClient(httpClient, cache, reporter, ImportCliTestData.JsonOptions, new StubTimeProvider(DateTimeOffset.Parse("2026-03-27T10:00:00Z")));
        var captureClient = new SentinelCaptureClient(httpClient, authClient, ImportCliTestData.JsonOptions);
        var service = new TwitterLikesImportService(
            new ArchiveInputResolver(),
            new TwitterLikesImportSource(),
            new TwitterLikeCaptureMapper(ImportCliTestData.JsonOptions),
            captureClient,
            reporter,
            new StubTimeProvider(DateTimeOffset.Parse("2026-03-27T10:00:00Z")));

        var output = new StringWriter();
        var error = new StringWriter();
        var cli = new CliApplication(service, output, error);

        var exitCode = await cli.InvokeAsync(["twitter", "likes", "--input", temp.Path, "--api-url", "https://sentinel.example"]);

        exitCode.Should().Be(0);
        handler.Logs.Should().ContainSingle(log => log.Method == "GET" && log.Path == "/api/v1/capture");
        handler.Logs.Should().NotContain(log => log.Method == "POST" && log.Path == "/api/v1/capture");
        output.ToString().Should().Contain("Duplicates skipped: 1");
        error.ToString().Should().BeEmpty();
    }
}
