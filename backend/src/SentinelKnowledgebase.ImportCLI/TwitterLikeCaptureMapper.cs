using System.Text.Json;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Application.DTOs.Labels;
using SentinelKnowledgebase.Domain.Enums;

namespace SentinelKnowledgebase.ImportCLI;

internal sealed class TwitterLikeCaptureMapper : ITwitterLikeCaptureMapper
{
    private readonly JsonSerializerOptions _jsonOptions;

    public TwitterLikeCaptureMapper(JsonSerializerOptions jsonOptions)
    {
        _jsonOptions = jsonOptions;
    }

    public CaptureRequestDto Map(TwitterLikeRecord like, TwitterArchiveMetadata metadata, DateTimeOffset importedAt)
    {
        var sourceUrl = NormalizeSourceUrl(like);
        var rawContent = string.IsNullOrWhiteSpace(like.FullText)
            ? $"Imported Twitter like {like.TweetId} from {sourceUrl}"
            : like.FullText.Trim();

        var captureMetadata = new TwitterLikeCaptureMetadata
        {
            TweetId = like.TweetId,
            ExpandedUrl = string.IsNullOrWhiteSpace(like.ExpandedUrl) ? sourceUrl : like.ExpandedUrl.Trim(),
            CapturedAt = importedAt,
            Archive = new ImportedArchiveContext
            {
                AccountId = metadata.AccountId,
                UserName = metadata.UserName,
                DisplayName = metadata.DisplayName,
                GenerationDate = metadata.GenerationDate,
                SizeBytes = metadata.SizeBytes
            }
        };

        return new CaptureRequestDto
        {
            SourceUrl = sourceUrl,
            ContentType = ContentType.Tweet,
            RawContent = rawContent,
            Metadata = JsonSerializer.Serialize(captureMetadata, _jsonOptions),
            Tags = ["twitter", "archive-import"],
            Labels =
            [
                new LabelAssignmentDto
                {
                    Category = "Source",
                    Value = "Twitter"
                }
            ]
        };
    }

    internal static string NormalizeSourceUrl(TwitterLikeRecord like)
    {
        if (Uri.TryCreate(like.ExpandedUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return uri.ToString();
        }

        return $"https://twitter.com/i/web/status/{Uri.EscapeDataString(like.TweetId)}";
    }
}
