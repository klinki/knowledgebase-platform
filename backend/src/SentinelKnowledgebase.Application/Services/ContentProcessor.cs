using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Localization;
using SentinelKnowledgebase.Domain.Enums;

namespace SentinelKnowledgebase.Application.Services;

public class ContentProcessor : IContentProcessor
{
    private const string StructuredOutputModeConfigKey = "OpenAI:StructuredOutputMode";
    private const string StructuredOutputModeAuto = "auto";
    private const string StructuredOutputModeJsonSchema = "json_schema";
    private const string StructuredOutputModeJsonObject = "json_object";
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly IMonitoringService _monitoringService;
    private readonly ILogger<ContentProcessor> _logger;
    
    public ContentProcessor(
        IConfiguration configuration,
        HttpClient httpClient,
        IMonitoringService monitoringService,
        ILogger<ContentProcessor> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _monitoringService = monitoringService;
        _logger = logger;
    }
    
    public string DenoiseContent(string content)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var meaningfulLines = lines
            .Where(line => !string.IsNullOrWhiteSpace(line) && 
                           !IsNoiseLine(line) &&
                           !IsOnlyPunctuation(line))
            .Select(line => line.Trim())
            .ToList();
        
        return string.Join("\n", meaningfulLines);
    }
    
    public async Task<ContentInsights> ExtractInsightsAsync(
        string content,
        ContentType contentType,
        string? outputLanguageCode = null)
    {
        var prompt = GeneratePrompt(content, contentType, outputLanguageCode);
        EnsureOpenAiApiKeyConfigured();
        return await CallOpenAIForInsights(prompt);
    }
    
    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            EnsureOpenAiApiKeyConfigured();
            
            var embeddingModel = _configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
            var apiKey = _configuration["OpenAI:ApiKey"];

            var requestBody = CreateEmbeddingRequestBody(embeddingModel, text);

            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", apiKey);
            
            var embeddingsUrl = _configuration["OpenAI:EmbeddingsUrl"] ?? "https://api.openai.com/v1/embeddings";
            var response = await _httpClient.PostAsJsonAsync(
                embeddingsUrl, requestBody);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadFromJsonAsync<OpenAIEmbeddingResponse>();
                var usage = responseContent?.usage;
                if (usage != null)
                {
                    _monitoringService.RecordAiTokenUsage(usage.prompt_tokens, 0, usage.total_tokens, "embedding");
                }

                var embedding = responseContent?.data?[0]?.embedding;
                if (embedding != null)
                {
                    return embedding.ToArray();
                }

                throw new InvalidOperationException("Embedding response did not contain an embedding vector.");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Embedding request failed with status {StatusCode}. Response: {ResponseBody}",
                (int)response.StatusCode,
                responseBody);

            throw new HttpRequestException(
                $"Embedding request failed with status {(int)response.StatusCode}.",
                null,
                response.StatusCode);
        }
        finally
        {
            stopwatch.Stop();
            _monitoringService.RecordEmbeddingGenerationLatency(stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    public async Task<ClusterMetadata> GenerateClusterMetadataAsync(IReadOnlyCollection<string> summaries)
    {
        var prompt = GenerateClusterPrompt(summaries);
        EnsureOpenAiApiKeyConfigured();
        return await CallOpenAIForClusterMetadata(prompt);
    }
    
    private string GeneratePrompt(string content, ContentType contentType, string? outputLanguageCode)
    {
        var typeSpecificInstructions = contentType switch
        {
            ContentType.Tweet => "Extract the main point, author, and any key takeaways from this tweet.",
            ContentType.Article => "Summarize the article, extract key points, and identify any actionable advice.",
            ContentType.Code => "Explain what this code does, its purpose, and any important technical details.",
            _ => "Extract the key information and insights from this content."
        };

        var languageInstruction = string.IsNullOrWhiteSpace(outputLanguageCode)
            ? "Keep the generated title, summary, key insights, and action items in the original language of the content. Do not translate SourceTitle or Author."
            : $"Write the generated title, summary, key insights, and action items in {LanguageCatalog.GetDisplayName(outputLanguageCode) ?? outputLanguageCode}. Keep SourceTitle and Author in the original language of the content and do not translate them.";
        
        return $@"Analyze the following content and provide a JSON response with:
1. A concise title (max 100 characters)
2. A summary (max 500 characters)
3. Key insights (list of up to 5 bullet points)
4. Action items (list of up to 3 actionable items if applicable)
5. Source title if available
6. Author name if available

{typeSpecificInstructions}
{languageInstruction}

Content:
{content}

Respond with valid JSON only.";
    }
    
    private async Task<ContentInsights> CallOpenAIForInsights(string prompt)
    {
        return await ExecuteStructuredChatCompletionAsync(
            prompt,
            "You are a helpful content analysis assistant. Always respond with valid JSON.",
            0.3,
            CreateContentInsightsResponseFormat(),
            ParseJsonToInsights,
            ValidateContentInsights,
            "Insight extraction");
    }

    private async Task<ClusterMetadata> CallOpenAIForClusterMetadata(string prompt)
    {
        return await ExecuteStructuredChatCompletionAsync(
            prompt,
            "You name semantic knowledge clusters. Always respond with valid JSON.",
            0.2,
            CreateClusterMetadataResponseFormat(),
            ParseJsonToClusterMetadata,
            ValidateClusterMetadata,
            "Cluster metadata");
    }

    private async Task<T> ExecuteStructuredChatCompletionAsync<T>(
        string prompt,
        string systemPrompt,
        double temperature,
        ResponseFormatRequest schemaResponseFormat,
        Func<string, T> parseResponse,
        Action<T> validateResponse,
        string operationName)
    {
        var mode = GetStructuredOutputMode();
        var initialFormat = mode == StructuredOutputModeJsonObject
            ? CreateJsonObjectResponseFormat()
            : schemaResponseFormat;
        var fallbackFormat = CreateJsonObjectResponseFormat();

        try
        {
            return await SendChatCompletionAsync(
                prompt,
                systemPrompt,
                temperature,
                initialFormat,
                parseResponse,
                validateResponse);
        }
        catch (StructuredOutputUnsupportedException) when (mode == StructuredOutputModeAuto && initialFormat.Type == StructuredOutputModeJsonSchema)
        {
            _logger.LogWarning(
                "{OperationName} structured output fallback activated because json_schema is unsupported for model {Model}. Retrying with json_object.",
                operationName,
                _configuration["OpenAI:Model"] ?? "gpt-4");
        }
        catch (Exception exception) when (
            mode == StructuredOutputModeAuto
            && initialFormat.Type == StructuredOutputModeJsonSchema
            && (exception is InvalidOperationException || exception is JsonException))
        {
            _logger.LogWarning(
                exception,
                "{OperationName} returned unusable schema-constrained JSON. Retrying with json_object fallback.",
                operationName);
        }

        return await SendChatCompletionAsync(
            prompt,
            $"{systemPrompt} Return only a JSON object. Do not include markdown, prose, comments, or code fences.",
            temperature,
            fallbackFormat,
            parseResponse,
            validateResponse);
    }

    private async Task<T> SendChatCompletionAsync<T>(
        string prompt,
        string systemPrompt,
        double temperature,
        ResponseFormatRequest responseFormat,
        Func<string, T> parseResponse,
        Action<T> validateResponse)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        var model = _configuration["OpenAI:Model"] ?? "gpt-4";

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = new ChatCompletionRequest
        {
            Model = model,
            Messages =
            [
                new ChatMessageRequest("system", systemPrompt),
                new ChatMessageRequest("user", prompt)
            ],
            Temperature = temperature,
            ResponseFormat = responseFormat
        };

        var chatCompletionsUrl = _configuration["OpenAI:ChatCompletionsUrl"] ?? "https://api.openai.com/v1/chat/completions";
        var response = await _httpClient.PostAsJsonAsync(chatCompletionsUrl, requestBody);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            if (responseFormat.Type == StructuredOutputModeJsonSchema
                && IsStructuredOutputUnsupported(response.StatusCode, responseBody))
            {
                throw new StructuredOutputUnsupportedException(response.StatusCode, responseBody);
            }

            _logger.LogWarning(
                "Chat completion request failed with status {StatusCode}. Response: {ResponseBody}",
                (int)response.StatusCode,
                responseBody);

            throw new HttpRequestException(
                $"Chat completion request failed with status {(int)response.StatusCode}.",
                null,
                response.StatusCode);
        }

        var responseContent = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>();
        var usage = responseContent?.usage;
        if (usage != null)
        {
            _monitoringService.RecordAiTokenUsage(
                usage.prompt_tokens,
                usage.completion_tokens,
                usage.total_tokens,
                "chat_completion");
        }

        var content = responseContent?.choices?[0].message?.content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Chat completions response did not contain a message content payload.");
        }

        var parsed = parseResponse(content);
        validateResponse(parsed);
        return parsed;
    }
    
    private ContentInsights ParseJsonToInsights(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var normalizedJson = NormalizeJsonPayload(json);

        try
        {
            return JsonSerializer.Deserialize<ContentInsights>(normalizedJson, options)
                ?? throw new InvalidOperationException("Chat completions response contained an empty JSON payload.");
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to parse content insights JSON. Raw response: {ResponseBody}",
                normalizedJson);

            throw;
        }
    }

    private ClusterMetadata ParseJsonToClusterMetadata(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var normalizedJson = NormalizeJsonPayload(json);

        try
        {
            return JsonSerializer.Deserialize<ClusterMetadata>(normalizedJson, options)
                ?? throw new InvalidOperationException("Cluster metadata response contained an empty JSON payload.");
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to parse cluster metadata JSON. Raw response: {ResponseBody}",
                normalizedJson);

            throw;
        }
    }

    private void EnsureOpenAiApiKeyConfigured()
    {
        if (!string.IsNullOrWhiteSpace(_configuration["OpenAI:ApiKey"]))
        {
            return;
        }

        _logger.LogError("OpenAI API key is not configured. Content processing cannot continue.");
        throw new InvalidOperationException("OpenAI API key is not configured.");
    }

    private Dictionary<string, object> CreateEmbeddingRequestBody(string embeddingModel, string text)
    {
        var requestBody = new Dictionary<string, object>
        {
            ["model"] = embeddingModel,
            ["input"] = text
        };

        var embeddingDimensions = GetEmbeddingDimensions();
        if (embeddingDimensions.HasValue)
        {
            requestBody["dimensions"] = embeddingDimensions.Value;
        }

        return requestBody;
    }

    private int? GetEmbeddingDimensions()
    {
        var configuredDimensions = _configuration["OpenAI:EmbeddingDimensions"];
        if (string.IsNullOrWhiteSpace(configuredDimensions))
        {
            return null;
        }

        if (int.TryParse(configuredDimensions, out var embeddingDimensions) && embeddingDimensions > 0)
        {
            return embeddingDimensions;
        }

        _logger.LogWarning(
            "OpenAI embedding dimensions value '{EmbeddingDimensions}' is invalid and will be ignored.",
            configuredDimensions);

        return null;
    }

    private static string NormalizeJsonPayload(string json)
    {
        var trimmedJson = json.Trim();
        if (!trimmedJson.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmedJson;
        }

        var firstNewLineIndex = trimmedJson.IndexOf('\n');
        if (firstNewLineIndex < 0)
        {
            return trimmedJson.Trim('`').Trim();
        }

        var withoutOpeningFence = trimmedJson[(firstNewLineIndex + 1)..];
        var lastFenceIndex = withoutOpeningFence.LastIndexOf("```", StringComparison.Ordinal);
        if (lastFenceIndex >= 0)
        {
            withoutOpeningFence = withoutOpeningFence[..lastFenceIndex];
        }

        return withoutOpeningFence.Trim();
    }

    private static ResponseFormatRequest CreateContentInsightsResponseFormat()
    {
        return new ResponseFormatRequest(
            "json_schema",
            new JsonSchemaRequest(
                "content_insights",
                true,
                new Dictionary<string, object?>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object?>
                    {
                        ["title"] = new Dictionary<string, object?> { ["type"] = "string" },
                        ["summary"] = new Dictionary<string, object?> { ["type"] = "string" },
                        ["keyInsights"] = new Dictionary<string, object?>
                        {
                            ["type"] = "array",
                            ["items"] = new Dictionary<string, object?> { ["type"] = "string" }
                        },
                        ["actionItems"] = new Dictionary<string, object?>
                        {
                            ["type"] = "array",
                            ["items"] = new Dictionary<string, object?> { ["type"] = "string" }
                        },
                        ["sourceTitle"] = new Dictionary<string, object?> { ["type"] = new object[] { "string", "null" } },
                        ["author"] = new Dictionary<string, object?> { ["type"] = new object[] { "string", "null" } }
                    },
                    ["required"] = new[] { "title", "summary", "keyInsights", "actionItems", "sourceTitle", "author" },
                    ["additionalProperties"] = false
                }));
    }

    private static ResponseFormatRequest CreateJsonObjectResponseFormat()
    {
        return new ResponseFormatRequest("json_object");
    }

    private static ResponseFormatRequest CreateClusterMetadataResponseFormat()
    {
        return new ResponseFormatRequest(
            "json_schema",
            new JsonSchemaRequest(
                "cluster_metadata",
                true,
                new Dictionary<string, object?>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object?>
                    {
                        ["title"] = new Dictionary<string, object?> { ["type"] = "string" },
                        ["description"] = new Dictionary<string, object?> { ["type"] = new object[] { "string", "null" } },
                        ["keywords"] = new Dictionary<string, object?>
                        {
                            ["type"] = "array",
                            ["items"] = new Dictionary<string, object?> { ["type"] = "string" }
                        }
                    },
                    ["required"] = new[] { "title", "description", "keywords" },
                    ["additionalProperties"] = false
                }));
    }

    private static string GenerateClusterPrompt(IReadOnlyCollection<string> summaries)
    {
        var joinedSummaries = string.Join("\n---\n", summaries);
        return $@"Given these related summaries, produce a JSON object with:
1. title: concise topic title, max 60 chars
2. description: one-line description, max 160 chars
3. keywords: array of exactly 3 short keywords

Summaries:
{joinedSummaries}

Respond with valid JSON only.";
    }

    private static void ValidateContentInsights(ContentInsights insights)
    {
        if (string.IsNullOrWhiteSpace(insights.Title))
        {
            throw new InvalidOperationException("Content insights response did not contain a title.");
        }

        if (string.IsNullOrWhiteSpace(insights.Summary))
        {
            throw new InvalidOperationException("Content insights response did not contain a summary.");
        }

        if (insights.KeyInsights == null)
        {
            throw new InvalidOperationException("Content insights response did not contain key insights.");
        }

        if (insights.ActionItems == null)
        {
            throw new InvalidOperationException("Content insights response did not contain action items.");
        }
    }

    private static void ValidateClusterMetadata(ClusterMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata.Title))
        {
            throw new InvalidOperationException("Cluster metadata response did not contain a title.");
        }

        if (metadata.Keywords == null || metadata.Keywords.Count == 0 || metadata.Keywords.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Cluster metadata response did not contain usable keywords.");
        }
    }

    private string GetStructuredOutputMode()
    {
        var configuredMode = _configuration[StructuredOutputModeConfigKey]?.Trim().ToLowerInvariant();
        return configuredMode switch
        {
            StructuredOutputModeJsonSchema => StructuredOutputModeJsonSchema,
            StructuredOutputModeJsonObject => StructuredOutputModeJsonObject,
            _ => StructuredOutputModeAuto
        };
    }

    private static bool IsStructuredOutputUnsupported(System.Net.HttpStatusCode statusCode, string responseBody)
    {
        if (statusCode != System.Net.HttpStatusCode.BadRequest
            && (int)statusCode != 422
            && statusCode != System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return false;
        }

        return responseBody.Contains("response_format", StringComparison.OrdinalIgnoreCase)
            || responseBody.Contains("json_schema", StringComparison.OrdinalIgnoreCase)
            || responseBody.Contains("structured output", StringComparison.OrdinalIgnoreCase)
            || responseBody.Contains("unsupported", StringComparison.OrdinalIgnoreCase);
    }
    
    private bool IsNoiseLine(string line)
    {
        var noisePatterns = new[] { "http", "www.", "retweet", "share", "follow", "@", "#" };
        return noisePatterns.Any(pattern => line.ToLower().Contains(pattern.ToLower())) && line.Length < 50;
    }
    
    private bool IsOnlyPunctuation(string line)
    {
        return line.All(c => char.IsPunctuation(c) || char.IsSymbol(c));
    }
    
    private class OpenAIEmbeddingResponse
    {
        public List<EmbeddingData>? data { get; set; }
        public OpenAiUsage? usage { get; set; }
    }
    
    private class EmbeddingData
    {
        public List<float>? embedding { get; set; }
    }
    
    private class OpenAIChatResponse
    {
        public List<ChatChoice>? choices { get; set; }
        public OpenAiUsage? usage { get; set; }
    }

    private class OpenAiUsage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }
    
    private class ChatChoice
    {
        public ChatMessage? message { get; set; }
    }
    
    private class ChatMessage
    {
        public string? content { get; set; }
    }

    private sealed class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<ChatMessageRequest> Messages { get; set; } = [];

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("response_format")]
        public ResponseFormatRequest ResponseFormat { get; set; } = null!;
    }

    private sealed record ChatMessageRequest(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed class ResponseFormatRequest
    {
        public ResponseFormatRequest(string type, JsonSchemaRequest? jsonSchema = null)
        {
            Type = type;
            JsonSchema = jsonSchema;
        }

        [JsonPropertyName("type")]
        public string Type { get; }

        [JsonPropertyName("json_schema")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonSchemaRequest? JsonSchema { get; }
    }

    private sealed record JsonSchemaRequest(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("strict")] bool Strict,
        [property: JsonPropertyName("schema")] Dictionary<string, object?> Schema);

    private sealed class StructuredOutputUnsupportedException : Exception
    {
        public StructuredOutputUnsupportedException(System.Net.HttpStatusCode statusCode, string responseBody)
            : base($"Structured output is unsupported for this route. Status {(int)statusCode}.")
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }

        public System.Net.HttpStatusCode StatusCode { get; }

        public string ResponseBody { get; }
    }
}
