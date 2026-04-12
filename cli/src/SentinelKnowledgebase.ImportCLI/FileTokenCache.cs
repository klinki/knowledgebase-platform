using System.Text.Json;

namespace SentinelKnowledgebase.ImportCLI;

internal sealed class FileTokenCache : ITokenCache
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _cachePath;

    public FileTokenCache(JsonSerializerOptions jsonOptions, string? cachePath = null)
    {
        _jsonOptions = jsonOptions;
        _cachePath = cachePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SentinelKnowledgebase",
            "import-cli-auth.json");
    }

    public async Task<CachedAuthSession?> GetAsync(string apiUrl, CancellationToken cancellationToken)
    {
        var document = await LoadAsync(cancellationToken);
        return document.Sessions.FirstOrDefault(session =>
            string.Equals(session.ApiUrl, ApiUrlNormalizer.Normalize(apiUrl), StringComparison.OrdinalIgnoreCase));
    }

    public async Task SaveAsync(CachedAuthSession session, CancellationToken cancellationToken)
    {
        var document = await LoadAsync(cancellationToken);
        var normalizedApiUrl = ApiUrlNormalizer.Normalize(session.ApiUrl);
        document.Sessions.RemoveAll(candidate =>
            string.Equals(candidate.ApiUrl, normalizedApiUrl, StringComparison.OrdinalIgnoreCase));

        session.ApiUrl = normalizedApiUrl;
        document.Sessions.Add(session);
        await SaveDocumentAsync(document, cancellationToken);
    }

    public async Task ClearAsync(string apiUrl, CancellationToken cancellationToken)
    {
        var document = await LoadAsync(cancellationToken);
        document.Sessions.RemoveAll(session =>
            string.Equals(session.ApiUrl, ApiUrlNormalizer.Normalize(apiUrl), StringComparison.OrdinalIgnoreCase));

        if (document.Sessions.Count == 0)
        {
            if (File.Exists(_cachePath))
            {
                File.Delete(_cachePath);
            }

            return;
        }

        await SaveDocumentAsync(document, cancellationToken);
    }

    private async Task<TokenCacheDocument> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_cachePath))
        {
            return new TokenCacheDocument();
        }

        try
        {
            var content = await File.ReadAllTextAsync(_cachePath, cancellationToken);
            return JsonSerializer.Deserialize<TokenCacheDocument>(content, _jsonOptions) ?? new TokenCacheDocument();
        }
        catch (Exception) when (File.Exists(_cachePath))
        {
            return new TokenCacheDocument();
        }
    }

    private async Task SaveDocumentAsync(TokenCacheDocument document, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_cachePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(document, _jsonOptions);
        await File.WriteAllTextAsync(_cachePath, json, cancellationToken);
    }
}
