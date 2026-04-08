using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using System.Text;
using System.Text.Json;
using SentinelKnowledgebase.Application.Services;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Enums;
using Xunit;

namespace SentinelKnowledgebase.UnitTests;

public class ContentProcessorTests
{
    [Fact]
    public void DenoiseContent_ShouldRemoveNoiseLines()
    {
        var processor = CreateProcessor();
        var content = @"
This is important content.
http://example.com
More important content.
#hashtag
Final important content.
";
        
        var result = processor.DenoiseContent(content);
        
        result.Should().NotContain("http://example.com");
        result.Should().NotContain("#hashtag");
        result.Should().Contain("This is important content.");
        result.Should().Contain("Final important content.");
    }
    
    [Fact]
    public void DenoiseContent_ShouldPreservePunctuationLines()
    {
        var processor = CreateProcessor();
        var content = @"Important content with punctuation! Another line.";
        
        var result = processor.DenoiseContent(content);
        
        result.Should().Contain("Important content");
    }
    
    [Fact]
    public async Task ExtractInsightsAsync_ShouldThrow_WhenApiKeyIsMissing()
    {
        var processor = CreateProcessor();
        var content = "This is a test article about programming. It covers best practices.";
        
        var action = async () => await processor.ExtractInsightsAsync(content, ContentType.Article);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("OpenAI API key is not configured.");
    }
    
    [Fact]
    public async Task GenerateEmbeddingAsync_ShouldThrow_WhenApiKeyIsMissing()
    {
        var processor = CreateProcessor();
        var text = "Test text for embedding";
        
        var action = async () => await processor.GenerateEmbeddingAsync(text);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("OpenAI API key is not configured.");
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_ShouldIncludeDimensions_WhenConfigured()
    {
        var handler = new RecordingHttpMessageHandler(CreateEmbeddingResponse());
        var processor = CreateProcessor(
            overrides: new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "test-key" },
                { "OpenAI:EmbeddingDimensions", "1536" }
            },
            httpClient: new HttpClient(handler));

        await processor.GenerateEmbeddingAsync("Test text for embedding");

        var payload = await ReadRequestPayloadAsync(handler.LastRequestMessage);
        payload.GetProperty("dimensions").GetInt32().Should().Be(1536);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_ShouldOmitDimensions_WhenNotConfigured()
    {
        var handler = new RecordingHttpMessageHandler(CreateEmbeddingResponse());
        var processor = CreateProcessor(
            overrides: new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "test-key" }
            },
            httpClient: new HttpClient(handler));

        await processor.GenerateEmbeddingAsync("Test text for embedding");

        var payload = await ReadRequestPayloadAsync(handler.LastRequestMessage);
        payload.TryGetProperty("dimensions", out _).Should().BeFalse();
    }

    private static ContentProcessor CreateProcessor(
        IDictionary<string, string?>? overrides = null,
        HttpClient? httpClient = null)
    {
        var configValues = new Dictionary<string, string?>
        {
            { "OpenAI:ApiKey", string.Empty },
            { "OpenAI:Model", "gpt-4" },
            { "OpenAI:EmbeddingModel", "text-embedding-3-small" },
            { "OpenAI:EmbeddingsUrl", "https://example.com/v1/embeddings" },
            { "OpenAI:ChatCompletionsUrl", "https://example.com/v1/chat/completions" }
        };

        if (overrides != null)
        {
            foreach (var (key, value) in overrides)
            {
                configValues[key] = value;
            }
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var monitoringService = Substitute.For<IMonitoringService>();
        var logger = Substitute.For<ILogger<ContentProcessor>>();

        return new ContentProcessor(
            config,
            httpClient ?? new HttpClient(),
            monitoringService,
            logger);
    }

    private static StringContent CreateEmbeddingResponse()
    {
        var payload = JsonSerializer.Serialize(new
        {
            data = new[]
            {
                new
                {
                    embedding = new[] { 0.1f, 0.2f, 0.3f }
                }
            },
            usage = new
            {
                prompt_tokens = 1,
                completion_tokens = 0,
                total_tokens = 1
            }
        });

        return new StringContent(payload, Encoding.UTF8, "application/json");
    }

    private static async Task<JsonElement> ReadRequestPayloadAsync(HttpRequestMessage? requestMessage)
    {
        requestMessage.Should().NotBeNull();
        requestMessage!.Content.Should().NotBeNull();

        var payload = await requestMessage.Content!.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        return document.RootElement.Clone();
    }

    private sealed class RecordingHttpMessageHandler(HttpContent responseContent) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequestMessage { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestMessage = request;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = responseContent
            });
        }
    }
}
