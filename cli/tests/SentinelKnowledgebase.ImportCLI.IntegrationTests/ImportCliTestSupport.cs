using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace SentinelKnowledgebase.ImportCLI.IntegrationTests;

internal sealed class TempDirectoryScope : IDisposable
{
    public TempDirectoryScope()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sentinel-import-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}

internal sealed class StubTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public StubTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow;
    }

    public void Advance(TimeSpan delta)
    {
        _utcNow = _utcNow.Add(delta);
    }
}

internal sealed class TestImportReporter : SentinelKnowledgebase.ImportCLI.IImportReporter
{
    public List<string> InfoMessages { get; } = [];
    public List<string> WarningMessages { get; } = [];
    public List<string> ErrorMessages { get; } = [];

    public void WriteInfo(string message)
    {
        InfoMessages.Add(message);
    }

    public void WriteWarning(string message)
    {
        WarningMessages.Add(message);
    }

    public void WriteError(string message)
    {
        ErrorMessages.Add(message);
    }
}

internal sealed record HttpRequestLog(string Method, string Path, string? Body, string? Authorization);

internal sealed class DelegateHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpRequestLog, HttpResponseMessage> _responseFactory;

    public DelegateHttpMessageHandler(Func<HttpRequestMessage, HttpRequestLog, HttpResponseMessage> responseFactory)
    {
        _responseFactory = responseFactory;
    }

    public List<HttpRequestLog> Logs { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content == null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);
        var log = new HttpRequestLog(
            request.Method.Method,
            request.RequestUri?.AbsolutePath ?? string.Empty,
            body,
            request.Headers.Authorization?.Parameter);
        Logs.Add(log);
        return _responseFactory(request, log);
    }
}

internal static class ImportCliTestData
{
    public static readonly JsonSerializerOptions JsonOptions = SentinelKnowledgebase.ImportCLI.JsonDefaults.Create();

    public static string CreateArchiveDirectory(string rootPath, string likePayloadJson)
    {
        var dataDirectory = System.IO.Path.Combine(rootPath, "data");
        Directory.CreateDirectory(dataDirectory);
        File.WriteAllText(System.IO.Path.Combine(dataDirectory, "manifest.js"), """
window.__THAR_CONFIG = {
  "userInfo": {
    "accountId": "27277579",
    "userName": "klinkicz",
    "displayName": "David Klingenberg"
  },
  "archiveInfo": {
    "sizeBytes": "87536692",
    "generationDate": "2026-02-02T22:57:57.777Z"
  }
};
""");
        File.WriteAllText(System.IO.Path.Combine(dataDirectory, "like.js"), $"window.YTD.like.part0 = {likePayloadJson};");
        return rootPath;
    }

    public static string CreateArchiveDirectoryWithManifestOnly(string rootPath)
    {
        var dataDirectory = System.IO.Path.Combine(rootPath, "data");
        Directory.CreateDirectory(dataDirectory);
        File.WriteAllText(System.IO.Path.Combine(dataDirectory, "manifest.js"), """
window.__THAR_CONFIG = {
  "userInfo": {
    "accountId": "27277579",
    "userName": "klinkicz",
    "displayName": "David Klingenberg"
  },
  "archiveInfo": {
    "sizeBytes": "87536692",
    "generationDate": "2026-02-02T22:57:57.777Z"
  }
};
""");
        return rootPath;
    }

    public static string SingleLikePayload(string tweetId = "2018256260119101805", string? fullText = "Imported tweet", string? expandedUrl = "https://twitter.com/i/web/status/2018256260119101805")
    {
        var safeFullText = fullText is null ? "null" : JsonSerializer.Serialize(fullText);
        var safeExpandedUrl = expandedUrl is null ? "null" : JsonSerializer.Serialize(expandedUrl);
        return $$"""
[
  {
    "like": {
      "tweetId": "{{tweetId}}",
      "fullText": {{safeFullText}},
      "expandedUrl": {{safeExpandedUrl}}
    }
  }
]
""";
    }

    public static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, object payload)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json")
        };
    }

    public static HttpResponseMessage TextResponse(HttpStatusCode statusCode, string payload)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(payload, Encoding.UTF8, "text/plain")
        };
    }
}
