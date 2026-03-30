using SentinelKnowledgebase.Application.DTOs.Capture;

namespace SentinelKnowledgebase.ImportCLI;

internal sealed class TwitterLikesImportService : ITwitterLikesImportService
{
    private const int DefaultProgressReportInterval = 500;
    private const int DefaultSubmissionBatchSize = 500;
    private readonly IArchiveInputResolver _archiveInputResolver;
    private readonly ITwitterArchiveImportSource _importSource;
    private readonly ITwitterLikeCaptureMapper _captureMapper;
    private readonly ISentinelCaptureClient _captureClient;
    private readonly IImportReporter _reporter;
    private readonly TimeProvider _timeProvider;
    private readonly int _progressReportInterval;
    private readonly int _submissionBatchSize;

    public TwitterLikesImportService(
        IArchiveInputResolver archiveInputResolver,
        ITwitterArchiveImportSource importSource,
        ITwitterLikeCaptureMapper captureMapper,
        ISentinelCaptureClient captureClient,
        IImportReporter reporter,
        TimeProvider timeProvider,
        int progressReportInterval = DefaultProgressReportInterval,
        int submissionBatchSize = DefaultSubmissionBatchSize)
    {
        _archiveInputResolver = archiveInputResolver;
        _importSource = importSource;
        _captureMapper = captureMapper;
        _captureClient = captureClient;
        _reporter = reporter;
        _timeProvider = timeProvider;
        _progressReportInterval = Math.Max(1, progressReportInterval);
        _submissionBatchSize = Math.Max(1, submissionBatchSize);
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
        _reporter.WriteInfo($"Starting import of {batch.Likes.Count} liked tweets...");

        var duplicateCount = 0;
        var successCount = 0;
        var failureCount = 0;
        var importedAt = _timeProvider.GetUtcNow();
        var startedAt = importedAt;
        var processedCount = 0;
        var stagedTweetIds = new HashSet<string>(StringComparer.Ordinal);
        var pendingLikes = new List<TwitterLikeRecord>(_submissionBatchSize);
        var pendingRequests = new List<CaptureRequestDto>(_submissionBatchSize);

        async Task FlushPendingBatchAsync()
        {
            if (pendingRequests.Count == 0)
            {
                return;
            }

            var submitResult = await _captureClient.CreateCapturesAsync(options.ApiUrl, pendingRequests, cancellationToken);
            var failedIndexes = submitResult.Failures
                .Select(failure => failure.RequestIndex)
                .ToHashSet();

            successCount += submitResult.SuccessfulCount;
            failureCount += submitResult.Failures.Count;

            for (var index = 0; index < pendingLikes.Count; index++)
            {
                if (!failedIndexes.Contains(index))
                {
                    existingTweetIds.Add(pendingLikes[index].TweetId);
                }
            }

            foreach (var failure in submitResult.Failures)
            {
                var like = pendingLikes[failure.RequestIndex];
                _reporter.WriteWarning($"Failed to submit tweet {like.TweetId}: {failure.ErrorMessage}");
            }

            pendingLikes.Clear();
            pendingRequests.Clear();
            stagedTweetIds.Clear();

            ReportProgressIfNeeded(
                processedCount,
                batch.Likes.Count,
                successCount,
                duplicateCount,
                failureCount,
                batch.MalformedRecords,
                startedAt);
        }

        foreach (var like in batch.Likes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            processedCount++;

            if (existingTweetIds.Contains(like.TweetId) || !stagedTweetIds.Add(like.TweetId))
            {
                duplicateCount++;
                ReportProgressIfNeeded(processedCount, batch.Likes.Count, successCount, duplicateCount, failureCount, batch.MalformedRecords, startedAt);
                continue;
            }

            pendingLikes.Add(like);
            pendingRequests.Add(_captureMapper.Map(like, batch.Metadata, importedAt));

            if (pendingRequests.Count >= _submissionBatchSize)
            {
                await FlushPendingBatchAsync();
            }
        }

        await FlushPendingBatchAsync();

        return new TwitterLikesImportSummary(
            TotalLikesRead: batch.TotalRecords,
            DuplicatesSkipped: duplicateCount,
            SuccessfulImports: successCount,
            FailedSubmissions: failureCount,
            MalformedRecords: batch.MalformedRecords);
    }

    private void ReportProgressIfNeeded(
        int processedCount,
        int totalCount,
        int successCount,
        int duplicateCount,
        int failureCount,
        int malformedCount,
        DateTimeOffset startedAt)
    {
        if (processedCount % _progressReportInterval != 0 && processedCount != totalCount)
        {
            return;
        }

        var progressPercent = totalCount == 0 ? 100 : (processedCount * 100d) / totalCount;
        var elapsed = _timeProvider.GetUtcNow() - startedAt;
        var elapsedText = elapsed <= TimeSpan.Zero ? "0s" : $"{elapsed.TotalSeconds:0}s";
        var etaText = FormatEta(processedCount, totalCount, elapsed);

        _reporter.WriteInfo(
            $"Progress: {processedCount}/{totalCount} ({progressPercent:0.0}%) | imported {successCount} | duplicates {duplicateCount} | failed {failureCount} | malformed {malformedCount} | elapsed {elapsedText} | eta {etaText}");
    }

    private static string FormatEta(int processedCount, int totalCount, TimeSpan elapsed)
    {
        if (processedCount <= 0 || totalCount <= processedCount || elapsed <= TimeSpan.Zero)
        {
            return "0s";
        }

        var averageSecondsPerItem = elapsed.TotalSeconds / processedCount;
        var remainingItems = totalCount - processedCount;
        var remainingSeconds = averageSecondsPerItem * remainingItems;
        var remaining = TimeSpan.FromSeconds(Math.Max(0, remainingSeconds));

        if (remaining.TotalHours >= 1)
        {
            return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
        }

        if (remaining.TotalMinutes >= 1)
        {
            return $"{(int)remaining.TotalMinutes}m {remaining.Seconds}s";
        }

        return $"{Math.Max(0, (int)Math.Round(remaining.TotalSeconds))}s";
    }
}
