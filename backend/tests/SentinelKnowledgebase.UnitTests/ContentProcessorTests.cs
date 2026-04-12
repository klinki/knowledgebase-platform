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

    [Fact]
    public async Task ExtractInsightsAsync_ShouldParseJsonWrappedInCodeFence()
    {
        var handler = new RecordingHttpMessageHandler(CreateChatResponse("""
            ```json
            {"title":"Test title","summary":"Test summary","keyInsights":["One"],"actionItems":["Act"],"sourceTitle":"Source","author":"Author"}
            ```
            """));
        var processor = CreateProcessor(
            overrides: new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "test-key" }
            },
            httpClient: new HttpClient(handler));

        var result = await processor.ExtractInsightsAsync("Article content", ContentType.Article);

        result.Title.Should().Be("Test title");
        result.Summary.Should().Be("Test summary");
    }

    [Fact]
    public async Task ExtractInsightsAsync_ShouldRequestStrictJsonSchemaResponseFormat()
    {
        var handler = new RecordingHttpMessageHandler(CreateChatResponse("""
            {"title":"Test title","summary":"Test summary","keyInsights":["One"],"actionItems":["Act"],"sourceTitle":"Source","author":"Author"}
            """));
        var processor = CreateProcessor(
            overrides: new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "test-key" }
            },
            httpClient: new HttpClient(handler));

        await processor.ExtractInsightsAsync("Article content", ContentType.Article);

        var payload = await ReadRequestPayloadAsync(handler.LastRequestMessage);
        payload.GetProperty("response_format").GetProperty("type").GetString().Should().Be("json_schema");
        var schema = payload.GetProperty("response_format").GetProperty("json_schema");
        schema.GetProperty("name").GetString().Should().Be("content_insights");
        schema.GetProperty("strict").GetBoolean().Should().BeTrue();
        var properties = schema.GetProperty("schema").GetProperty("properties");
        properties.TryGetProperty("title", out _).Should().BeTrue();
        properties.TryGetProperty("summary", out _).Should().BeTrue();
        properties.TryGetProperty("keyInsights", out _).Should().BeTrue();
        properties.TryGetProperty("actionItems", out _).Should().BeTrue();
        properties.TryGetProperty("sourceTitle", out _).Should().BeTrue();
        properties.TryGetProperty("author", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateClusterMetadataAsync_ShouldRequestStrictJsonSchemaResponseFormat()
    {
        var handler = new RecordingHttpMessageHandler(CreateChatResponse("""
            {"title":"AI Infrastructure","description":"Serving and deployment notes.","keywords":["ai","infra","serving"]}
            """));
        var processor = CreateProcessor(
            overrides: new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "test-key" }
            },
            httpClient: new HttpClient(handler));

        var result = await processor.GenerateClusterMetadataAsync(["One summary", "Another summary"]);

        result.Title.Should().Be("AI Infrastructure");
        var payload = await ReadRequestPayloadAsync(handler.LastRequestMessage);
        payload.GetProperty("response_format").GetProperty("type").GetString().Should().Be("json_schema");
        var schema = payload.GetProperty("response_format").GetProperty("json_schema");
        schema.GetProperty("name").GetString().Should().Be("cluster_metadata");
        schema.GetProperty("strict").GetBoolean().Should().BeTrue();
        var properties = schema.GetProperty("schema").GetProperty("properties");
        properties.TryGetProperty("title", out _).Should().BeTrue();
        properties.TryGetProperty("description", out _).Should().BeTrue();
        properties.TryGetProperty("keywords", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ExtractInsightsAsync_ShouldFallbackToJsonObject_WhenJsonSchemaIsUnsupported()
    {
        var handler = new RecordingHttpMessageHandler(
            CreateErrorResponse(HttpStatusCode.BadRequest, """
                {"error":{"message":"response_format json_schema is unsupported for this model"}}
                """),
            CreateSuccessResponse(CreateChatResponse("""
                {"title":"Test title","summary":"Test summary","keyInsights":["One"],"actionItems":["Act"],"sourceTitle":"Source","author":"Author"}
                """)));
        var processor = CreateProcessor(
            overrides: new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "test-key" }
            },
            httpClient: new HttpClient(handler));

        var result = await processor.ExtractInsightsAsync("Article content", ContentType.Article);

        result.Title.Should().Be("Test title");
        handler.RequestMessages.Should().HaveCount(2);
        var firstPayload = await ReadRequestPayloadAsync(handler.RequestMessages[0]);
        firstPayload.GetProperty("response_format").GetProperty("type").GetString().Should().Be("json_schema");
        var secondPayload = await ReadRequestPayloadAsync(handler.RequestMessages[1]);
        secondPayload.GetProperty("response_format").GetProperty("type").GetString().Should().Be("json_object");
        secondPayload.GetProperty("response_format").TryGetProperty("json_schema", out _).Should().BeFalse();
    }

    [Fact]
    public async Task ExtractInsightsAsync_ShouldFallbackToJsonObject_WhenSchemaResponseIsInvalid()
    {
        var handler = new RecordingHttpMessageHandler(
            CreateSuccessResponse(CreateChatResponse("""
                {"title":"","summary":"Test summary","keyInsights":["One"],"actionItems":["Act"],"sourceTitle":"Source","author":"Author"}
                """)),
            CreateSuccessResponse(CreateChatResponse("""
                {"title":"Recovered title","summary":"Recovered summary","keyInsights":["One"],"actionItems":["Act"],"sourceTitle":"Source","author":"Author"}
                """)));
        var processor = CreateProcessor(
            overrides: new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "test-key" }
            },
            httpClient: new HttpClient(handler));

        var result = await processor.ExtractInsightsAsync("Article content", ContentType.Article);

        result.Title.Should().Be("Recovered title");
        handler.RequestMessages.Should().HaveCount(2);
        var secondPayload = await ReadRequestPayloadAsync(handler.RequestMessages[1]);
        secondPayload.GetProperty("response_format").GetProperty("type").GetString().Should().Be("json_object");
    }

    [Fact]
    public async Task ExtractInsightsAsync_ShouldAllowForcingJsonObjectMode()
    {
        var handler = new RecordingHttpMessageHandler(CreateChatResponse("""
            {"title":"Test title","summary":"Test summary","keyInsights":["One"],"actionItems":["Act"],"sourceTitle":"Source","author":"Author"}
            """));
        var processor = CreateProcessor(
            overrides: new Dictionary<string, string?>
            {
                { "OpenAI:ApiKey", "test-key" },
                { "OpenAI:StructuredOutputMode", "json_object" }
            },
            httpClient: new HttpClient(handler));

        await processor.ExtractInsightsAsync("Article content", ContentType.Article);

        var payload = await ReadRequestPayloadAsync(handler.LastRequestMessage);
        payload.GetProperty("response_format").GetProperty("type").GetString().Should().Be("json_object");
        payload.GetProperty("response_format").TryGetProperty("json_schema", out _).Should().BeFalse();
    }

    private static ContentProcessor CreateProcessor(
        IDictionary<string, string?>? overrides = null,
        HttpClient? httpClient = null)
    {
        var configValues = new Dictionary<string, string?>
        {
            { "OpenAI:ApiKey", string.Empty },
            { "OpenAI:Model", "gpt-4" },
            { "OpenAI:StructuredOutputMode", "auto" },
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

    private static StringContent CreateChatResponse(string assistantContent)
    {
        var payload = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = assistantContent
                    }
                }
            },
            usage = new
            {
                prompt_tokens = 1,
                completion_tokens = 1,
                total_tokens = 2
            }
        });

        return new StringContent(payload, Encoding.UTF8, "application/json");
    }

    private static HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, string payload)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage CreateSuccessResponse(HttpContent payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = payload
        };
    }

    private static async Task<JsonElement> ReadRequestPayloadAsync(HttpRequestMessage? requestMessage)
    {
        requestMessage.Should().NotBeNull();
        requestMessage!.Content.Should().NotBeNull();

        var payload = await requestMessage.Content!.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        return document.RootElement.Clone();
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public RecordingHttpMessageHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        public RecordingHttpMessageHandler(HttpContent responseContent)
            : this(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = responseContent
            })
        {
        }

        public HttpRequestMessage? LastRequestMessage { get; private set; }
        public List<HttpRequestMessage> RequestMessages { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestMessage = request;
            RequestMessages.Add(request);

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No queued HTTP response is available for this request.");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
