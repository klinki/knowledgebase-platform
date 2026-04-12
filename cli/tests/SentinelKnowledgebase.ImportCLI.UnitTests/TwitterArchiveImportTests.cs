using AwesomeAssertions;
using NSubstitute;
using SentinelKnowledgebase.Application.DTOs.Capture;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Domain.Services;
using SentinelKnowledgebase.ImportCLI;
using System.IO.Compression;
using System.Text.Json;
using Xunit;

namespace SentinelKnowledgebase.ImportCLI.UnitTests;

public class TwitterArchiveImportTests
{
    [Theory]
    [InlineData("This Post is from a suspended account. {learnmore}", "twitter_suspended_account")]
    [InlineData("You’re unable to view this Post because this account owner limits who can view their Posts. {learnmore}", "twitter_account_limited")]
    [InlineData("This Post is unavailable. {learnmore}", "twitter_post_unavailable")]
    public void TwitterPlaceholderContentFilter_TryMatch_ShouldRecognizeKnownPlaceholderText(string content, string expectedCode)
    {
        var matched = TwitterPlaceholderContentFilter.TryMatch($"  {content}  ", out var match);

        matched.Should().BeTrue();
        match.Code.Should().Be(expectedCode);
        match.Reason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void TwitterPlaceholderContentFilter_TryMatch_ShouldIgnoreNormalTweetText()
    {
        var matched = TwitterPlaceholderContentFilter.TryMatch("Ship the feature and write the migration notes.", out _);

        matched.Should().BeFalse();
    }

    [Fact]
    public async Task TwitterLikesImportSource_ReadAsync_FromDirectory_ShouldParseLikes()
    {
        using var temp = new TempDirectoryScope();
        ImportCliTestData.CreateArchiveDirectory(temp.Path, ImportCliTestData.SingleLikePayload(fullText: "Directory like"));

        var resolver = new ArchiveInputResolver();
        var source = new TwitterLikesImportSource();

        await using var archive = await resolver.ResolveAsync(temp.Path, CancellationToken.None);
        var result = await source.ReadAsync(archive, CancellationToken.None);

        result.TotalRecords.Should().Be(1);
        result.MalformedRecords.Should().Be(0);
        result.Likes.Should().ContainSingle();
        result.Likes[0].TweetId.Should().Be("2018256260119101805");
        result.Likes[0].FullText.Should().Be("Directory like");
        result.Metadata.UserName.Should().Be("klinkicz");
    }

    [Fact]
    public async Task TwitterLikesImportSource_ReadAsync_FromZip_ShouldParseLikes()
    {
        using var temp = new TempDirectoryScope();
        var archiveDirectory = System.IO.Path.Combine(temp.Path, "twitter-archive");
        Directory.CreateDirectory(archiveDirectory);
        ImportCliTestData.CreateArchiveDirectory(archiveDirectory, ImportCliTestData.SingleLikePayload(fullText: "Zip like"));

        var zipPath = System.IO.Path.Combine(temp.Path, "twitter-archive.zip");
        ZipFile.CreateFromDirectory(archiveDirectory, zipPath);

        var resolver = new ArchiveInputResolver();
        var source = new TwitterLikesImportSource();

        await using var archive = await resolver.ResolveAsync(zipPath, CancellationToken.None);
        var result = await source.ReadAsync(archive, CancellationToken.None);

        result.TotalRecords.Should().Be(1);
        result.Likes[0].FullText.Should().Be("Zip like");
    }

    [Fact]
    public async Task TwitterLikesImportSource_ReadAsync_WithoutLikeFile_ShouldFail()
    {
        using var temp = new TempDirectoryScope();
        ImportCliTestData.CreateArchiveDirectoryWithManifestOnly(temp.Path);

        var resolver = new ArchiveInputResolver();
        var source = new TwitterLikesImportSource();

        await using var archive = await resolver.ResolveAsync(temp.Path, CancellationToken.None);
        var act = async () => await source.ReadAsync(archive, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*data/like.js*");
    }

    [Fact]
    public async Task TwitterLikesImportSource_ReadAsync_WithMalformedRows_ShouldSkipInvalidEntries()
    {
        using var temp = new TempDirectoryScope();
        ImportCliTestData.CreateArchiveDirectory(temp.Path, """
[
  {
    "like": {
      "tweetId": "1",
      "fullText": "Valid row",
      "expandedUrl": "https://twitter.com/i/web/status/1"
    }
  },
  {
    "like": {
      "fullText": "Missing tweet id"
    }
  },
  {
    "notLike": {
      "tweetId": "3"
    }
  }
]
""");

        var resolver = new ArchiveInputResolver();
        var source = new TwitterLikesImportSource();

        await using var archive = await resolver.ResolveAsync(temp.Path, CancellationToken.None);
        var result = await source.ReadAsync(archive, CancellationToken.None);

        result.TotalRecords.Should().Be(3);
        result.MalformedRecords.Should().Be(2);
        result.Likes.Should().ContainSingle();
        result.Likes[0].TweetId.Should().Be("1");
    }

    [Fact]
    public void TwitterLikeCaptureMapper_Map_ShouldPopulateCaptureRequest()
    {
        var mapper = new TwitterLikeCaptureMapper(ImportCliTestData.JsonOptions);
        var metadata = new TwitterArchiveMetadata("27277579", "klinkicz", "David Klingenberg", DateTimeOffset.Parse("2026-02-02T22:57:57.777Z"), "87536692");
        var like = new TwitterLikeRecord("2018256260119101805", "Mapped tweet", "https://twitter.com/i/web/status/2018256260119101805");
        var importedAt = DateTimeOffset.Parse("2026-03-27T10:00:00Z");

        var result = mapper.Map(like, metadata, importedAt);

        result.ContentType.Should().Be(ContentType.Tweet);
        result.SourceUrl.Should().Be("https://twitter.com/i/web/status/2018256260119101805");
        result.RawContent.Should().Be("Mapped tweet");
        result.Tags.Should().BeEquivalentTo(["twitter", "archive-import"]);
        result.Labels.Should().ContainSingle(label => label.Category == "Source" && label.Value == "Twitter");

        using var metadataDocument = JsonDocument.Parse(result.Metadata!);
        metadataDocument.RootElement.GetProperty("source").GetString().Should().Be("twitter");
        metadataDocument.RootElement.GetProperty("importSource").GetString().Should().Be("twitter_archive_like");
        metadataDocument.RootElement.GetProperty("deferClustering").GetBoolean().Should().BeTrue();
        metadataDocument.RootElement.GetProperty("tweetId").GetString().Should().Be("2018256260119101805");
        metadataDocument.RootElement.GetProperty("capturedAt").GetDateTimeOffset().Should().Be(importedAt);
        metadataDocument.RootElement.GetProperty("archive").GetProperty("userName").GetString().Should().Be("klinkicz");
    }

    [Fact]
    public void TwitterLikeCaptureMapper_Map_WithEmptyText_ShouldFallbackToSafeRawContent()
    {
        var mapper = new TwitterLikeCaptureMapper(ImportCliTestData.JsonOptions);
        var metadata = new TwitterArchiveMetadata(null, null, null, null, null);
        var like = new TwitterLikeRecord("999", "", null);

        var result = mapper.Map(like, metadata, DateTimeOffset.Parse("2026-03-27T10:00:00Z"));

        result.RawContent.Should().Contain("999");
        result.RawContent.Should().Contain("https://twitter.com/i/web/status/999");
    }

    [Fact]
    public async Task TwitterLikesImportService_ImportAsync_ShouldSkipDuplicatesAndCountFailures()
    {
        var archive = Substitute.For<IArchiveDataSource>();
        archive.DisplayName.Returns("test-archive");

        var resolver = Substitute.For<IArchiveInputResolver>();
        resolver.ResolveAsync("archive", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(archive));

        var source = Substitute.For<ITwitterArchiveImportSource>();
        source.ReadAsync(archive, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TwitterArchiveLikeBatch(
                new TwitterArchiveMetadata(null, null, null, null, null),
                [
                    new TwitterLikeRecord("1", "Duplicate", "https://twitter.com/i/web/status/1"),
                    new TwitterLikeRecord("2", "Success", "https://twitter.com/i/web/status/2"),
                    new TwitterLikeRecord("3", "Failure", "https://twitter.com/i/web/status/3")
                ],
                TotalRecords: 3,
                MalformedRecords: 1)));

        var mapper = Substitute.For<ITwitterLikeCaptureMapper>();
        mapper.Map(Arg.Any<TwitterLikeRecord>(), Arg.Any<TwitterArchiveMetadata>(), Arg.Any<DateTimeOffset>())
            .Returns(call => new CaptureRequestDto
            {
                SourceUrl = $"https://twitter.com/i/web/status/{call.Arg<TwitterLikeRecord>().TweetId}",
                ContentType = ContentType.Tweet,
                RawContent = call.Arg<TwitterLikeRecord>().FullText ?? string.Empty
            });

        var captureClient = Substitute.For<ISentinelCaptureClient>();
        captureClient.GetExistingTweetIdsAsync("https://sentinel.example", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HashSet<string>(StringComparer.Ordinal) { "1" }));
        captureClient.CreateCapturesAsync("https://sentinel.example", Arg.Any<IReadOnlyList<CaptureRequestDto>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SubmitBulkCapturesResult(
                1,
                [new SubmitBulkCaptureFailure(1, "boom")])));
        captureClient.TriggerClusterRebuildAsync("https://sentinel.example", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TriggerClusterRebuildResult(true)));

        var reporter = new TestImportReporter();
        var service = new TwitterLikesImportService(
            resolver,
            source,
            mapper,
            captureClient,
            reporter,
            new StubTimeProvider(DateTimeOffset.Parse("2026-03-27T10:00:00Z")),
            submissionBatchSize: 2);

        var result = await service.ImportAsync(
            new TwitterLikesImportOptions("archive", "https://sentinel.example"),
            CancellationToken.None);

        result.TotalLikesRead.Should().Be(3);
        result.DuplicatesSkipped.Should().Be(1);
        result.FilteredPlaceholders.Should().Be(0);
        result.SuccessfulImports.Should().Be(1);
        result.FailedSubmissions.Should().Be(1);
        result.MalformedRecords.Should().Be(1);
        reporter.WarningMessages.Should().ContainSingle(message => message.Contains("tweet 3"));
        await captureClient.Received(1).CreateCapturesAsync(
            "https://sentinel.example",
            Arg.Any<IReadOnlyList<CaptureRequestDto>>(),
            Arg.Any<CancellationToken>());
        await captureClient.Received(1).TriggerClusterRebuildAsync("https://sentinel.example", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TwitterLikesImportService_ImportAsync_ShouldFilterPlaceholderTweetsBeforeSubmission()
    {
        var archive = Substitute.For<IArchiveDataSource>();
        archive.DisplayName.Returns("test-archive");

        var resolver = Substitute.For<IArchiveInputResolver>();
        resolver.ResolveAsync("archive", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(archive));

        var source = Substitute.For<ITwitterArchiveImportSource>();
        source.ReadAsync(archive, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TwitterArchiveLikeBatch(
                new TwitterArchiveMetadata(null, null, null, null, null),
                [
                    new TwitterLikeRecord("1", "This Post is unavailable. {learnmore}", "https://twitter.com/i/web/status/1"),
                    new TwitterLikeRecord("2", "Normal archive tweet", "https://twitter.com/i/web/status/2")
                ],
                TotalRecords: 2,
                MalformedRecords: 0)));

        var mapper = Substitute.For<ITwitterLikeCaptureMapper>();
        mapper.Map(Arg.Any<TwitterLikeRecord>(), Arg.Any<TwitterArchiveMetadata>(), Arg.Any<DateTimeOffset>())
            .Returns(call => new CaptureRequestDto
            {
                SourceUrl = $"https://twitter.com/i/web/status/{call.Arg<TwitterLikeRecord>().TweetId}",
                ContentType = ContentType.Tweet,
                RawContent = call.Arg<TwitterLikeRecord>().FullText ?? string.Empty
            });

        var captureClient = Substitute.For<ISentinelCaptureClient>();
        IReadOnlyList<CaptureRequestDto>? submittedRequests = null;
        captureClient.GetExistingTweetIdsAsync("https://sentinel.example", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HashSet<string>(StringComparer.Ordinal)));
        captureClient.CreateCapturesAsync("https://sentinel.example", Arg.Any<IReadOnlyList<CaptureRequestDto>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                submittedRequests = call.Arg<IReadOnlyList<CaptureRequestDto>>().ToList();
                return Task.FromResult(new SubmitBulkCapturesResult(1, []));
            });
        captureClient.TriggerClusterRebuildAsync("https://sentinel.example", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TriggerClusterRebuildResult(true)));

        var reporter = new TestImportReporter();
        var service = new TwitterLikesImportService(
            resolver,
            source,
            mapper,
            captureClient,
            reporter,
            new StubTimeProvider(DateTimeOffset.Parse("2026-03-27T10:00:00Z")),
            submissionBatchSize: 10);

        var result = await service.ImportAsync(
            new TwitterLikesImportOptions("archive", "https://sentinel.example"),
            CancellationToken.None);

        result.TotalLikesRead.Should().Be(2);
        result.DuplicatesSkipped.Should().Be(0);
        result.FilteredPlaceholders.Should().Be(1);
        result.SuccessfulImports.Should().Be(1);
        result.FailedSubmissions.Should().Be(0);
        await captureClient.Received(1).CreateCapturesAsync(
            "https://sentinel.example",
            Arg.Any<IReadOnlyList<CaptureRequestDto>>(),
            Arg.Any<CancellationToken>());
        submittedRequests.Should().NotBeNull();
        submittedRequests.Should().ContainSingle();
        submittedRequests![0].SourceUrl.Should().Be("https://twitter.com/i/web/status/2");
        submittedRequests[0].RawContent.Should().Be("Normal archive tweet");
        await captureClient.Received(1).TriggerClusterRebuildAsync("https://sentinel.example", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TwitterLikesImportService_ImportAsync_ShouldReportProgressWithEta()
    {
        var archive = Substitute.For<IArchiveDataSource>();
        archive.DisplayName.Returns("test-archive");

        var resolver = Substitute.For<IArchiveInputResolver>();
        resolver.ResolveAsync("archive", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(archive));

        var source = Substitute.For<ITwitterArchiveImportSource>();
        source.ReadAsync(archive, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TwitterArchiveLikeBatch(
                new TwitterArchiveMetadata(null, null, null, null, null),
                [
                    new TwitterLikeRecord("1", "One", "https://twitter.com/i/web/status/1"),
                    new TwitterLikeRecord("2", "Two", "https://twitter.com/i/web/status/2"),
                    new TwitterLikeRecord("3", "Three", "https://twitter.com/i/web/status/3")
                ],
                TotalRecords: 3,
                MalformedRecords: 0)));

        var mapper = Substitute.For<ITwitterLikeCaptureMapper>();
        mapper.Map(Arg.Any<TwitterLikeRecord>(), Arg.Any<TwitterArchiveMetadata>(), Arg.Any<DateTimeOffset>())
            .Returns(call => new CaptureRequestDto
            {
                SourceUrl = $"https://twitter.com/i/web/status/{call.Arg<TwitterLikeRecord>().TweetId}",
                ContentType = ContentType.Tweet,
                RawContent = call.Arg<TwitterLikeRecord>().FullText ?? string.Empty
            });

        var captureClient = Substitute.For<ISentinelCaptureClient>();
        captureClient.GetExistingTweetIdsAsync("https://sentinel.example", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HashSet<string>(StringComparer.Ordinal)));
        var timeProvider = new StubTimeProvider(DateTimeOffset.Parse("2026-03-27T10:00:00Z"));
        captureClient.CreateCapturesAsync("https://sentinel.example", Arg.Any<IReadOnlyList<CaptureRequestDto>>(), Arg.Any<CancellationToken>())
            .Returns(
                callInfo =>
                {
                    timeProvider.Advance(TimeSpan.FromSeconds(20));
                    return Task.FromResult(new SubmitBulkCapturesResult(2, []));
                },
                callInfo =>
                {
                    timeProvider.Advance(TimeSpan.FromSeconds(10));
                    return Task.FromResult(new SubmitBulkCapturesResult(1, []));
                });
        captureClient.TriggerClusterRebuildAsync("https://sentinel.example", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TriggerClusterRebuildResult(true)));

        var reporter = new TestImportReporter();
        var service = new TwitterLikesImportService(
            resolver,
            source,
            mapper,
            captureClient,
            reporter,
            timeProvider,
            progressReportInterval: 2,
            submissionBatchSize: 2);

        await service.ImportAsync(
            new TwitterLikesImportOptions("archive", "https://sentinel.example"),
            CancellationToken.None);

        reporter.InfoMessages.Should().Contain(message => message.Contains("Starting import of 3 liked tweets"));
        reporter.InfoMessages.Should().Contain(message => message.Contains("Progress: 2/3") && message.Contains("eta 10s"));
        reporter.InfoMessages.Should().Contain(message => message.Contains("Progress: 3/3") && message.Contains("eta 0s"));
        reporter.InfoMessages.Should().Contain(message => message.Contains("Queued a single cluster rebuild after archive import."));
    }
}

