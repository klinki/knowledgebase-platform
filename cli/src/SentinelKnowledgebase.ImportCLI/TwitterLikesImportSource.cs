using System.Text.Json;

namespace SentinelKnowledgebase.ImportCLI;

internal sealed class TwitterLikesImportSource : ITwitterArchiveImportSource
{
    private readonly JavaScriptDataExtractor _extractor;

    public TwitterLikesImportSource()
        : this(new JavaScriptDataExtractor())
    {
    }

    public TwitterLikesImportSource(JavaScriptDataExtractor extractor)
    {
        _extractor = extractor;
    }

    public string Name => "likes";

    public async Task<TwitterArchiveLikeBatch> ReadAsync(IArchiveDataSource archive, CancellationToken cancellationToken)
    {
        var manifestText = await archive.ReadTextAsync("data/manifest.js", cancellationToken);
        var manifest = ParseManifest(manifestText);

        var likesText = await archive.ReadTextAsync("data/like.js", cancellationToken);
        return ParseLikes(manifest, likesText);
    }

    private TwitterArchiveMetadata ParseManifest(string manifestText)
    {
        var payload = _extractor.ExtractJsonPayload(manifestText, "data/manifest.js");
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        return new TwitterArchiveMetadata(
            GetNestedString(root, "userInfo", "accountId"),
            GetNestedString(root, "userInfo", "userName"),
            GetNestedString(root, "userInfo", "displayName"),
            ParseDateTimeOffset(GetNestedString(root, "archiveInfo", "generationDate")),
            GetNestedString(root, "archiveInfo", "sizeBytes"));
    }

    private TwitterArchiveLikeBatch ParseLikes(TwitterArchiveMetadata metadata, string likesText)
    {
        var payload = _extractor.ExtractJsonPayload(likesText, "data/like.js");
        using var document = JsonDocument.Parse(payload);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Twitter likes dataset is not a JSON array.");
        }

        var likes = new List<TwitterLikeRecord>();
        var totalRecords = 0;
        var malformedRecords = 0;

        foreach (var row in document.RootElement.EnumerateArray())
        {
            totalRecords++;

            if (!TryParseLike(row, out var like))
            {
                malformedRecords++;
                continue;
            }

            likes.Add(like!);
        }

        return new TwitterArchiveLikeBatch(metadata, likes, totalRecords, malformedRecords);
    }

    private static bool TryParseLike(JsonElement row, out TwitterLikeRecord? like)
    {
        like = null;
        if (row.ValueKind != JsonValueKind.Object || !row.TryGetProperty("like", out var likeElement))
        {
            return false;
        }

        var tweetId = GetString(likeElement, "tweetId");
        if (string.IsNullOrWhiteSpace(tweetId))
        {
            return false;
        }

        like = new TwitterLikeRecord(
            tweetId.Trim(),
            GetString(likeElement, "fullText"),
            GetString(likeElement, "expandedUrl"));
        return true;
    }

    private static string? GetNestedString(JsonElement root, string parentProperty, string propertyName)
    {
        if (!root.TryGetProperty(parentProperty, out var parent) || parent.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return GetString(parent, propertyName);
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static DateTimeOffset? ParseDateTimeOffset(string? value)
    {
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }
}

internal sealed class JavaScriptDataExtractor
{
    public string ExtractJsonPayload(string fileContents, string fileName)
    {
        var assignmentIndex = fileContents.IndexOf('=');
        if (assignmentIndex < 0)
        {
            throw new InvalidOperationException($"Twitter archive file '{fileName}' does not contain a JavaScript assignment wrapper.");
        }

        var payload = fileContents[(assignmentIndex + 1)..].Trim();
        if (payload.EndsWith(';'))
        {
            payload = payload[..^1].TrimEnd();
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new InvalidOperationException($"Twitter archive file '{fileName}' does not contain a JSON payload.");
        }

        return payload;
    }
}
