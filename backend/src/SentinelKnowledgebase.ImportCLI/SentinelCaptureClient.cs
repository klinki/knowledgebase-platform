using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SentinelKnowledgebase.Application.DTOs.Capture;

namespace SentinelKnowledgebase.ImportCLI;

internal sealed class SentinelCaptureClient : ISentinelCaptureClient
{
    private readonly HttpClient _httpClient;
    private readonly DeviceAuthClient _authClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public SentinelCaptureClient(
        HttpClient httpClient,
        DeviceAuthClient authClient,
        JsonSerializerOptions jsonOptions)
    {
        _httpClient = httpClient;
        _authClient = authClient;
        _jsonOptions = jsonOptions;
    }

    public async Task<HashSet<string>> GetExistingTweetIdsAsync(string apiUrl, CancellationToken cancellationToken)
    {
        using var response = await SendAuthorizedAsync(
            apiUrl,
            accessToken => BuildRequest(HttpMethod.Get, $"{ApiUrlNormalizer.Normalize(apiUrl)}/api/v1/capture", accessToken),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Failed to load existing captures: {(int)response.StatusCode} {errorText}");
        }

        var captures = await response.Content.ReadFromJsonAsync<List<CaptureResponseDto>>(_jsonOptions, cancellationToken) ?? [];
        var tweetIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var capture in captures)
        {
            var tweetId = CaptureMetadataReader.TryGetTwitterTweetId(capture.Metadata);
            if (!string.IsNullOrWhiteSpace(tweetId))
            {
                tweetIds.Add(tweetId);
            }
        }

        return tweetIds;
    }

    public async Task<SubmitCaptureResult> CreateCaptureAsync(string apiUrl, CaptureRequestDto request, CancellationToken cancellationToken)
    {
        using var response = await SendAuthorizedAsync(
            apiUrl,
            accessToken =>
            {
                var message = BuildRequest(HttpMethod.Post, $"{ApiUrlNormalizer.Normalize(apiUrl)}/api/v1/capture", accessToken);
                message.Content = JsonContent.Create(request, options: _jsonOptions);
                return message;
            },
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return new SubmitCaptureResult(true);
        }

        var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
        return new SubmitCaptureResult(false, $"API returned {(int)response.StatusCode}: {errorText}");
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(
        string apiUrl,
        Func<string, HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        var accessToken = await _authClient.GetAccessTokenAsync(apiUrl, cancellationToken);
        var response = await _httpClient.SendAsync(requestFactory(accessToken), cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        response.Dispose();
        await _authClient.InvalidateSessionAsync(apiUrl, cancellationToken);
        var refreshedToken = await _authClient.GetAccessTokenAsync(apiUrl, cancellationToken);
        return await _httpClient.SendAsync(requestFactory(refreshedToken), cancellationToken);
    }

    private static HttpRequestMessage BuildRequest(HttpMethod method, string url, string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }
}

internal static class CaptureMetadataReader
{
    public static string? TryGetTwitterTweetId(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(metadata);
            var root = document.RootElement;
            var source = GetString(root, "source");
            if (!string.Equals(source, "twitter", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return GetString(root, "tweetId") ?? GetString(root, "tweet_id");
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }
}

internal static class ApiUrlNormalizer
{
    public static string Normalize(string apiUrl)
    {
        return apiUrl.Trim().TrimEnd('/');
    }
}
