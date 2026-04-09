using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SentinelKnowledgebase.Application.Localization;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Enums;

namespace SentinelKnowledgebase.Application.Services;

public class ContentProcessor : IContentProcessor
{
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
        var apiKey = _configuration["OpenAI:ApiKey"];
        var model = _configuration["OpenAI:Model"] ?? "gpt-4";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        
        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "You are a helpful content analysis assistant. Always respond with valid JSON." },
                new { role = "user", content = prompt }
            },
            temperature = 0.3
        };
        
        var chatCompletionsUrl = _configuration["OpenAI:ChatCompletionsUrl"] ?? "https://api.openai.com/v1/chat/completions";
        var response = await _httpClient.PostAsJsonAsync(
            chatCompletionsUrl, requestBody);
        
        if (response.IsSuccessStatusCode)
        {
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
            
            if (!string.IsNullOrEmpty(content))
            {
                return ParseJsonToInsights(content);
            }

            throw new InvalidOperationException("Chat completions response did not contain a message content payload.");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        _logger.LogWarning(
            "Insight extraction request failed with status {StatusCode}. Response: {ResponseBody}",
            (int)response.StatusCode,
            responseBody);

        throw new HttpRequestException(
            $"Insight extraction request failed with status {(int)response.StatusCode}.",
            null,
            response.StatusCode);
    }

    private async Task<ClusterMetadata> CallOpenAIForClusterMetadata(string prompt)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        var model = _configuration["OpenAI:Model"] ?? "gpt-4";

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = "You name semantic knowledge clusters. Always respond with valid JSON." },
                new { role = "user", content = prompt }
            },
            temperature = 0.2
        };

        var chatCompletionsUrl = _configuration["OpenAI:ChatCompletionsUrl"] ?? "https://api.openai.com/v1/chat/completions";
        var response = await _httpClient.PostAsJsonAsync(chatCompletionsUrl, requestBody);
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>();
            var content = responseContent?.choices?[0].message?.content;
            if (!string.IsNullOrWhiteSpace(content))
            {
                var normalizedJson = NormalizeJsonPayload(content);
                return JsonSerializer.Deserialize<ClusterMetadata>(normalizedJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException("Cluster metadata response contained an empty JSON payload.");
            }

            throw new InvalidOperationException("Chat completions response did not contain a cluster metadata payload.");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        _logger.LogWarning(
            "Cluster metadata request failed with status {StatusCode}. Response: {ResponseBody}",
            (int)response.StatusCode,
            responseBody);

        throw new HttpRequestException(
            $"Cluster metadata request failed with status {(int)response.StatusCode}.",
            null,
            response.StatusCode);
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
}
