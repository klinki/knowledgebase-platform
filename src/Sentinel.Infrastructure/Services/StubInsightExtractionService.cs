using System.Text.RegularExpressions;
using Sentinel.Application.Interfaces;
using Sentinel.Application.Models;
using Sentinel.Domain.Enums;

namespace Sentinel.Infrastructure.Services;

public sealed class StubInsightExtractionService : IInsightExtractionService
{
    private static readonly Regex WordSplit = new(@"[^a-zA-Z0-9]+", RegexOptions.Compiled);
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "with", "that", "this", "from", "they", "you", "your", "for", "are", "was", "were",
        "but", "not", "have", "has", "had", "about", "into", "over", "when", "what", "how", "why", "who"
    };
    private static readonly HashSet<string> PositiveWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "great", "good", "love", "win", "success", "positive", "excellent", "amazing"
    };
    private static readonly HashSet<string> NegativeWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "bad", "fail", "loss", "negative", "terrible", "awful", "risk"
    };

    public Task<InsightExtractionResult> ExtractAsync(string cleanText, CancellationToken cancellationToken)
    {
        var summary = BuildSummary(cleanText);
        var insight = string.IsNullOrWhiteSpace(summary)
            ? "No insight extracted from empty content."
            : $"Key takeaway: {summary}";

        var sentiment = DetermineSentiment(cleanText);
        var tags = ExtractTags(cleanText);

        var result = new InsightExtractionResult
        {
            Summary = summary,
            Insight = insight,
            Sentiment = sentiment,
            Tags = tags
        };

        return Task.FromResult(result);
    }

    private static string BuildSummary(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var trimmed = text.Trim();
        if (trimmed.Length <= 160)
        {
            return trimmed;
        }

        return string.Concat(trimmed.AsSpan(0, 160), "...");
    }

    private static Sentiment DetermineSentiment(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Sentiment.Neutral;
        }

        var lower = text.ToLowerInvariant();
        var score = 0;

        foreach (var word in PositiveWords)
        {
            if (lower.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                score++;
            }
        }

        foreach (var word in NegativeWords)
        {
            if (lower.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                score--;
            }
        }

        if (score > 0)
        {
            return Sentiment.Positive;
        }

        if (score < 0)
        {
            return Sentiment.Negative;
        }

        return Sentiment.Neutral;
    }

    private static IReadOnlyCollection<string> ExtractTags(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new[] { "general" };
        }

        var tags = WordSplit
            .Split(text)
            .Select(word => word.Trim().ToLowerInvariant())
            .Where(word => word.Length > 3)
            .Where(word => !StopWords.Contains(word))
            .Distinct()
            .Take(5)
            .ToArray();

        return tags.Length == 0 ? new[] { "general" } : tags;
    }
}
