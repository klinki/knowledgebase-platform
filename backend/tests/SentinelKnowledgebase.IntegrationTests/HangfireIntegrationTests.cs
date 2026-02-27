using FluentAssertions;
using Hangfire;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SentinelKnowledgebase.Application.Services.Interfaces;
using Xunit;

namespace SentinelKnowledgebase.IntegrationTests;

[Collection("IntegrationTests")]
public class HangfireIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public HangfireIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Dashboard_ShouldBeAccessibleInDevelopment()
    {
        using var factory = _fixture.CreateApplicationFactory("Development");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/hangfire");

        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.OK,
            System.Net.HttpStatusCode.Unauthorized,
            System.Net.HttpStatusCode.MovedPermanently,
            System.Net.HttpStatusCode.Redirect,
            System.Net.HttpStatusCode.RedirectMethod,
            System.Net.HttpStatusCode.TemporaryRedirect);
    }

    [Fact]
    public void EnqueuedJob_ShouldPersistAcrossFactoryRestart()
    {
        string jobId;

        using (var firstFactory = _fixture.CreateApplicationFactory("Testing"))
        {
            using var scope = firstFactory.Services.CreateScope();
            var backgroundJobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();

            jobId = backgroundJobClient.Enqueue<ICaptureService>(service => service.ProcessCaptureAsync(Guid.NewGuid()));
            jobId.Should().NotBeNullOrWhiteSpace();
        }

        using var secondFactory = _fixture.CreateApplicationFactory("Testing");
        using var secondScope = secondFactory.Services.CreateScope();
        var storage = secondScope.ServiceProvider.GetRequiredService<JobStorage>();

        var persistedJob = storage.GetConnection().GetJobData(jobId);
        persistedJob.Should().NotBeNull();
        persistedJob!.State.Should().Be("Enqueued");
    }

    [Fact]
    public async Task FailedJob_ShouldRetryAndSucceed()
    {
        RetryProbeJob.Reset();

        using var factory = _fixture.CreateApplicationFactory("Development");
        using var scope = factory.Services.CreateScope();
        var backgroundJobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
        var storage = scope.ServiceProvider.GetRequiredService<JobStorage>();

        var jobId = backgroundJobClient.Enqueue(() => RetryProbeJob.RunOnceWithTransientFailure());
        jobId.Should().NotBeNullOrWhiteSpace();

        string? state = null;
        for (var i = 0; i < 60; i++)
        {
            state = storage.GetConnection().GetJobData(jobId)?.State;
            if (state == "Succeeded")
            {
                break;
            }

            await Task.Delay(500);
        }

        state.Should().Be("Succeeded");
        RetryProbeJob.AttemptCount.Should().BeGreaterThanOrEqualTo(2);
    }

    public static class RetryProbeJob
    {
        private static int _attemptCount;

        public static int AttemptCount => _attemptCount;

        public static void Reset()
        {
            Interlocked.Exchange(ref _attemptCount, 0);
        }

        public static Task RunOnceWithTransientFailure()
        {
            var attempt = Interlocked.Increment(ref _attemptCount);
            if (attempt == 1)
            {
                throw new InvalidOperationException("Transient failure for retry validation.");
            }

            return Task.CompletedTask;
        }
    }
}
