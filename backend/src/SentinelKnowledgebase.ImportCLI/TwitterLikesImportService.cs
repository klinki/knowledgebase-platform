namespace SentinelKnowledgebase.ImportCLI;

internal sealed class TwitterLikesImportService : ITwitterLikesImportService
{
    private readonly IArchiveInputResolver _archiveInputResolver;
    private readonly ITwitterArchiveImportSource _importSource;
    private readonly ITwitterLikeCaptureMapper _captureMapper;
    private readonly ISentinelCaptureClient _captureClient;
    private readonly IImportReporter _reporter;
    private readonly TimeProvider _timeProvider;

    public TwitterLikesImportService(
        IArchiveInputResolver archiveInputResolver,
        ITwitterArchiveImportSource importSource,
        ITwitterLikeCaptureMapper captureMapper,
        ISentinelCaptureClient captureClient,
        IImportReporter reporter,
        TimeProvider timeProvider)
    {
        _archiveInputResolver = archiveInputResolver;
        _importSource = importSource;
        _captureMapper = captureMapper;
        _captureClient = captureClient;
        _reporter = reporter;
        _timeProvider = timeProvider;
    }

    public async Task<TwitterLikesImportSummary> ImportAsync(
        TwitterLikesImportOptions options,
        CancellationToken cancellationToken)
    {
        await using var archive = await _archiveInputResolver.ResolveAsync(options.InputPath, cancellationToken);
        var batch = await _importSource.ReadAsync(archive, cancellationToken);

        _reporter.WriteInfo($"Loaded {batch.Likes.Count} valid liked tweets from '{archive.DisplayName}'.");

        var existingTweetIds = await _captureClient.GetExistingTweetIdsAsync(options.ApiUrl, cancellationToken);
        _reporter.WriteInfo($"Found {existingTweetIds.Count} existing imported or captured Twitter tweets for dedupe.");

        var duplicateCount = 0;
        var successCount = 0;
        var failureCount = 0;
        var importedAt = _timeProvider.GetUtcNow();

        foreach (var like in batch.Likes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (existingTweetIds.Contains(like.TweetId))
            {
                duplicateCount++;
                continue;
            }

            var captureRequest = _captureMapper.Map(like, batch.Metadata, importedAt);
            var result = await _captureClient.CreateCaptureAsync(options.ApiUrl, captureRequest, cancellationToken);

            if (result.Success)
            {
                successCount++;
                existingTweetIds.Add(like.TweetId);
                continue;
            }

            failureCount++;
            _reporter.WriteWarning($"Failed to submit tweet {like.TweetId}: {result.ErrorMessage}");
        }

        return new TwitterLikesImportSummary(
            TotalLikesRead: batch.TotalRecords,
            DuplicatesSkipped: duplicateCount,
            SuccessfulImports: successCount,
            FailedSubmissions: failureCount,
            MalformedRecords: batch.MalformedRecords);
    }
}
