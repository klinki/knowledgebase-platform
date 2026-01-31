using System.Text.RegularExpressions;
using Sentinel.Application.Interfaces;

namespace Sentinel.Application.Services;

public sealed class TextCleaner : ITextCleaner
{
    private static readonly Regex ThreadPattern = new(@"thread\s*\d+/\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex UrlPattern = new(@"https?://\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex WhitespacePattern = new(@"\s+", RegexOptions.Compiled);

    public string Clean(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var cleaned = ThreadPattern.Replace(input, string.Empty);
        cleaned = UrlPattern.Replace(cleaned, match => StripQuery(match.Value));
        cleaned = WhitespacePattern.Replace(cleaned, " ");

        return cleaned.Trim();
    }

    private static string StripQuery(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return url;
        }

        if (string.IsNullOrEmpty(uri.Query))
        {
            return url;
        }

        var builder = new UriBuilder(uri)
        {
            Query = string.Empty
        };

        return builder.Uri.ToString();
    }
}
