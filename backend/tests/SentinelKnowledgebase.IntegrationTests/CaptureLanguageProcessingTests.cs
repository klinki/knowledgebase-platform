using System.Net;
using System.Net.Http.Json;

using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using SentinelKnowledgebase.Application.DTOs.Auth;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Services;
using SentinelKnowledgebase.Infrastructure.Authentication;
using SentinelKnowledgebase.Infrastructure.Data;

using Xunit;

namespace SentinelKnowledgebase.IntegrationTests;

[Collection("IntegrationTests")]
public class CaptureLanguageProcessingTests
{
    private readonly IntegrationTestFixture _fixture;

    public CaptureLanguageProcessingTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("""{"source":"webpage","metadata":{"language":"fr-FR"}}""", "de", "", "de")]
    [InlineData("""{"source":"webpage","metadata":{"language":"fr-FR"}}""", "de", "fr", "fr")]
    [InlineData("""{"source":"webpage"}""", "de", "", "source")]
    [InlineData("""{"source":"webpage","metadata":{"language":"de-DE"}}""", "de", "", "de")]
    public async Task ProcessCaptureAsync_ShouldGenerateInsightsUsingResolvedOutputLanguage(
        string metadata,
        string defaultLanguageCode,
        string preservedLanguageCodes,
        string expectedLanguageMarker)
    {
        FakeContentProcessor.ResetObservations();

        var member = await _fixture.CreateMemberClientAsync();
        using var client = member.Client;
        var userId = await _fixture.GetUserIdByEmailAsync(member.Email);

        await ConfigurePreferencesAsync(
            userId,
            defaultLanguageCode,
            preservedLanguageCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        var createResponse = await client.PostAsJsonAsync("/api/v1/capture", new CaptureRequestDto
        {
            SourceUrl = $"https://example.com/language/{Guid.NewGuid():N}",
            ContentType = Domain.Enums.ContentType.Article,
            RawContent = "Bonjour tout le monde",
            Metadata = metadata
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var accepted = await createResponse.Content.ReadFromJsonAsync<CaptureAcceptedDto>();
        accepted.Should().NotBeNull();

        using (var scope = _fixture.CreateScope())
        {
            var captureService = scope.ServiceProvider.GetRequiredService<ICaptureService>();
            await captureService.ProcessCaptureAsync(accepted!.Id);
        }

        ProcessedInsight? processedInsight = null;
        using (var scope = _fixture.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            processedInsight = await dbContext.ProcessedInsights
                .SingleAsync(item => item.RawCaptureId == accepted!.Id);
        }

        processedInsight.Should().NotBeNull();
        processedInsight!.Title.Should().Contain($"[{expectedLanguageMarker}]");
        processedInsight.Summary.Should().Be($"[{expectedLanguageMarker}] Bonjour tout le monde");
        processedInsight.SourceTitle.Should().Be("Original title for Article");
        processedInsight.Author.Should().Be("Original author");
        FakeContentProcessor.GetEmbeddingInputs().Should().Contain(processedInsight.Summary);
    }

    private async Task ConfigurePreferencesAsync(
        Guid userId,
        string defaultLanguageCode,
        IReadOnlyCollection<string> preservedLanguageCodes)
    {
        using var scope = _fixture.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IUserLanguagePreferencesService>();
        await service.UpdateAsync(userId, defaultLanguageCode, preservedLanguageCodes);
    }
}
