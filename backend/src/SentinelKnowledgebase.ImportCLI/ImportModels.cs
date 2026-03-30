using SentinelKnowledgebase.Application.DTOs.Auth;
using SentinelKnowledgebase.Application.DTOs.Capture;

namespace SentinelKnowledgebase.ImportCLI;

public sealed record TwitterLikesImportOptions(string InputPath, string ApiUrl);

public sealed record TwitterLikesImportSummary(
    int TotalLikesRead,
    int DuplicatesSkipped,
    int SuccessfulImports,
    int FailedSubmissions,
    int MalformedRecords);

internal sealed record SubmitCaptureResult(bool Success, string? ErrorMessage = null);
internal sealed record SubmitBulkCaptureFailure(int RequestIndex, string ErrorMessage);
internal sealed record SubmitBulkCapturesResult(int SuccessfulCount, IReadOnlyList<SubmitBulkCaptureFailure> Failures);

internal sealed record TwitterArchiveMetadata(
    string? AccountId,
    string? UserName,
    string? DisplayName,
    DateTimeOffset? GenerationDate,
    string? SizeBytes);

internal sealed record TwitterLikeRecord(
    string TweetId,
    string? FullText,
    string? ExpandedUrl);

internal sealed record TwitterArchiveLikeBatch(
    TwitterArchiveMetadata Metadata,
    IReadOnlyList<TwitterLikeRecord> Likes,
    int TotalRecords,
    int MalformedRecords);

internal sealed class CachedAuthSession
{
    public string ApiUrl { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public AuthUserDto User { get; set; } = new();
}

internal sealed class TokenCacheDocument
{
    public List<CachedAuthSession> Sessions { get; set; } = [];
}

internal sealed class TwitterLikeCaptureMetadata
{
    public string Source { get; set; } = "twitter";
    public string ImportSource { get; set; } = "twitter_archive_like";
    public string TweetId { get; set; } = string.Empty;
    public string? ExpandedUrl { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
    public ImportedArchiveContext Archive { get; set; } = new();
}

internal sealed class ImportedArchiveContext
{
    public string? AccountId { get; set; }
    public string? UserName { get; set; }
    public string? DisplayName { get; set; }
    public DateTimeOffset? GenerationDate { get; set; }
    public string? SizeBytes { get; set; }
}

internal interface ITwitterLikesImportService
{
    Task<TwitterLikesImportSummary> ImportAsync(TwitterLikesImportOptions options, CancellationToken cancellationToken);
}

internal interface IArchiveInputResolver
{
    Task<IArchiveDataSource> ResolveAsync(string inputPath, CancellationToken cancellationToken);
}

internal interface IArchiveDataSource : IAsyncDisposable
{
    string DisplayName { get; }
    Task<string> ReadTextAsync(string relativePath, CancellationToken cancellationToken);
}

internal interface ITwitterArchiveImportSource
{
    string Name { get; }
    Task<TwitterArchiveLikeBatch> ReadAsync(IArchiveDataSource archive, CancellationToken cancellationToken);
}

internal interface ITwitterLikeCaptureMapper
{
    CaptureRequestDto Map(TwitterLikeRecord like, TwitterArchiveMetadata metadata, DateTimeOffset importedAt);
}

internal interface ISentinelCaptureClient
{
    Task<HashSet<string>> GetExistingTweetIdsAsync(string apiUrl, CancellationToken cancellationToken);
    Task<SubmitCaptureResult> CreateCaptureAsync(string apiUrl, CaptureRequestDto request, CancellationToken cancellationToken);
    Task<SubmitBulkCapturesResult> CreateCapturesAsync(string apiUrl, IReadOnlyList<CaptureRequestDto> requests, CancellationToken cancellationToken);
}

internal interface ITokenCache
{
    Task<CachedAuthSession?> GetAsync(string apiUrl, CancellationToken cancellationToken);
    Task SaveAsync(CachedAuthSession session, CancellationToken cancellationToken);
    Task ClearAsync(string apiUrl, CancellationToken cancellationToken);
}
