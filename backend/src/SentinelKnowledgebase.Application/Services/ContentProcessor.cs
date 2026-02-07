using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SentinelKnowledgebase.Application.Services.Interfaces;
using SentinelKnowledgebase.Domain.Enums;
using System.Net.Http.Json;

namespace SentinelKnowledgebase.Application.Services;

public class ContentProcessor : IContentProcessor
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public ContentProcessor(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
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

    public async Task<ContentInsights> ExtractInsightsAsync(string content, ContentType contentType)
    {
        var prompt = GeneratePrompt(content, contentType);

        var insights = new ContentInsights();

        if (!string.IsNullOrEmpty(_configuration["OpenAI:ApiKey"]))
        {
            insights = await CallOpenAIForInsights(prompt);
        }
        else
        {
            insights = GenerateFallbackInsights(content, contentType);
        }

        return insights;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        if (string.IsNullOrEmpty(_configuration["OpenAI:ApiKey"]))
        {
            return GenerateRandomEmbedding();
        }

        var embeddingModel = _configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
        var apiKey = _configuration["OpenAI:ApiKey"];

        var requestBody = new
        {
            model = embeddingModel,
            input = text
        };

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var response = await _httpClient.PostAsJsonAsync(
            "https://api.openai.com/v1/embeddings", requestBody);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadFromJsonAsync<OpenAIEmbeddingResponse>();
            var embedding = responseContent?.data?[0]?.embedding;
            return embedding != null ? embedding.ToArray() : GenerateRandomEmbedding();
        }

        return GenerateRandomEmbedding();
    }

    private string GeneratePrompt(string content, ContentType contentType)
    {
        var typeSpecificInstructions = contentType switch
        {
            ContentType.Tweet => "Extract the main point, author, and any key takeaways from this tweet.",
            ContentType.Article => "Summarize the article, extract key points, and identify any actionable advice.",
            ContentType.Code => "Explain what this code does, its purpose, and any important technical details.",
            _ => "Extract the key information and insights from this content."
        };

        return $@"Analyze the following content and provide a JSON response with:
1. A concise title (max 100 characters)
2. A summary (max 500 characters)
3. Key insights (list of up to 5 bullet points)
4. Action items (list of up to 3 actionable items if applicable)
5. Source title if available
6. Author name if available

{typeSpecificInstructions}

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

        var response = await _httpClient.PostAsJsonAsync(
            "https://api.openai.com/v1/chat/completions", requestBody);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>();
            var content = responseContent?.choices?[0].message?.content;

            if (!string.IsNullOrEmpty(content))
            {
                return ParseJsonToInsights(content);
            }
        }

        return new ContentInsights { Summary = "Failed to process content" };
    }

    private ContentInsights ParseJsonToInsights(string json)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<ContentInsights>(json, options) ?? new ContentInsights();
        }
        catch
        {
            return new ContentInsights { Summary = json.Length > 500 ? json[..500] : json };
        }
    }

    private ContentInsights GenerateFallbackInsights(string content, ContentType contentType)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var firstMeaningfulLine = lines.FirstOrDefault(l => !IsNoiseLine(l)) ?? content[..Math.Min(100, content.Length)];

        return new ContentInsights
        {
            Title = firstMeaningfulLine.Length > 100 ? firstMeaningfulLine[..100] : firstMeaningfulLine,
            Summary = content.Length > 500 ? content[..500] : content,
            KeyInsights = lines.Take(5).Where(l => !IsNoiseLine(l)).ToList(),
            ActionItems = new List<string>()
        };
    }

    private float[] GenerateRandomEmbedding()
    {
        // Use Random.Shared for non-deterministic random values
        // This ensures different embeddings for different content when OpenAI is unavailable
        var vector = new float[1536];
        for (int i = 0; i < vector.Length; i++)
        {
            vector[i] = (float)Random.Shared.NextDouble() * 2 - 1;
        }
        return NormalizeVector(vector);
    }

    private float[] NormalizeVector(float[] vector)
    {
        var norm = (float)Math.Sqrt(vector.Sum(x => x * x));
        return norm > 0 ? vector.Select(x => x / norm).ToArray() : vector;
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
    }

    private class EmbeddingData
    {
        public List<float>? embedding { get; set; }
    }

    private class OpenAIChatResponse
    {
        public List<ChatChoice>? choices { get; set; }
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
