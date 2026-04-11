using System.Text.RegularExpressions;

namespace SentinelKnowledgebase.Domain.Services;

public sealed record TwitterPlaceholderMatch(string Code, string Reason);

public static partial class TwitterPlaceholderContentFilter
{
    private static readonly IReadOnlyList<TwitterPlaceholderMatchRule> Rules =
    [
        new(
            "twitter_suspended_account",
            "Twitter post is inaccessible because the account is suspended.",
            "This Post is from a suspended account. {learnmore}"),
        new(
            "twitter_account_limited",
            "Twitter post is inaccessible because the account limits visibility.",
            "You’re unable to view this Post because this account owner limits who can view their Posts. {learnmore}"),
        new(
            "twitter_post_unavailable",
            "Twitter post is unavailable.",
            "This Post is unavailable. {learnmore}")
    ];

    public static bool TryMatch(string? content, out TwitterPlaceholderMatch match)
    {
        var normalized = Normalize(content);
        foreach (var rule in Rules)
        {
            if (string.Equals(normalized, rule.NormalizedText, StringComparison.OrdinalIgnoreCase))
            {
                match = new TwitterPlaceholderMatch(rule.Code, rule.Reason);
                return true;
            }
        }

        match = null!;
        return false;
    }

    public static string Normalize(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        return WhitespacePattern().Replace(content.Trim(), " ");
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();

    private sealed record TwitterPlaceholderMatchRule(string Code, string Reason, string Text)
    {
        public string NormalizedText { get; } = Normalize(Text);
    }
}
