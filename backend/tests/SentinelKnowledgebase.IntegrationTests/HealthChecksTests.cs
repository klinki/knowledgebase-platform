using FluentAssertions;
using Xunit;

namespace SentinelKnowledgebase.IntegrationTests;

[Collection("IntegrationTests")]
public class HealthChecksTests : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _httpClient;

    public HealthChecksTests(IntegrationTestFixture fixture)
    {
        _httpClient = fixture.HttpClient;
    }

    [Fact]
    public async Task GetHealth_ShouldReturn200Ok()
    {
        var response = await _httpClient.GetAsync("/health");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}
