using System.Globalization;

namespace SentinelKnowledgebase.Domain.Localization;

public sealed class SupportedLanguageDefinition
{
    public string Code { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}

public static class LanguageCatalog
{
    public const string FallbackLanguageCode = "en";

    private static readonly SupportedLanguageDefinition[] Definitions =
    [
        new() { Code = "cs", DisplayName = "Czech" },
        new() { Code = "de", DisplayName = "German" },
        new() { Code = "en", DisplayName = "English" },
        new() { Code = "es", DisplayName = "Spanish" },
        new() { Code = "fr", DisplayName = "French" },
        new() { Code = "it", DisplayName = "Italian" },
        new() { Code = "ja", DisplayName = "Japanese" },
        new() { Code = "ko", DisplayName = "Korean" },
        new() { Code = "nl", DisplayName = "Dutch" },
        new() { Code = "pl", DisplayName = "Polish" },
        new() { Code = "pt", DisplayName = "Portuguese" },
        new() { Code = "ru", DisplayName = "Russian" },
        new() { Code = "sv", DisplayName = "Swedish" },
        new() { Code = "tr", DisplayName = "Turkish" },
        new() { Code = "uk", DisplayName = "Ukrainian" },
        new() { Code = "zh", DisplayName = "Chinese" }
    ];

    private static readonly IReadOnlyDictionary<string, SupportedLanguageDefinition> DefinitionsByCode =
        Definitions.ToDictionary(definition => definition.Code, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<string, string> Aliases = BuildAliases();

    public static IReadOnlyList<SupportedLanguageDefinition> SupportedLanguages => Definitions;

    public static string ResolveSupportedLanguageCode(string? acceptLanguageHeader)
    {
        if (!string.IsNullOrWhiteSpace(acceptLanguageHeader))
        {
            foreach (var token in acceptLanguageHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var languageToken = token.Split(';', StringSplitOptions.TrimEntries)[0];
                var normalized = NormalizeSupportedLanguageCode(languageToken);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    return normalized;
                }
            }
        }

        return FallbackLanguageCode;
    }

    public static string? NormalizeSupportedLanguageCode(string? value)
    {
        var normalized = NormalizeToBaseLanguageCode(value);
        if (normalized == null)
        {
            return null;
        }

        return DefinitionsByCode.ContainsKey(normalized)
            ? normalized
            : null;
    }

    public static string? NormalizeToBaseLanguageCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var candidate = value.Trim();
        if (Aliases.TryGetValue(candidate, out var aliasedCode))
        {
            return aliasedCode;
        }

        var normalizedCandidate = candidate.Replace('_', '-');
        var baseSegment = normalizedCandidate.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
        if (baseSegment.Length is >= 2 and <= 8 && baseSegment.All(char.IsLetter))
        {
            return baseSegment.ToLowerInvariant();
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(normalizedCandidate);
            var code = culture.TwoLetterISOLanguageName.ToLowerInvariant();
            return code == "iv" ? null : code;
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    public static string? GetDisplayName(string? languageCode)
    {
        var normalized = NormalizeToBaseLanguageCode(languageCode);
        if (normalized == null)
        {
            return null;
        }

        if (DefinitionsByCode.TryGetValue(normalized, out var supported))
        {
            return supported.DisplayName;
        }

        try
        {
            return CultureInfo.GetCultureInfo(normalized).EnglishName;
        }
        catch (CultureNotFoundException)
        {
            return normalized;
        }
    }

    private static IReadOnlyDictionary<string, string> BuildAliases()
    {
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in Definitions)
        {
            aliases[definition.Code] = definition.Code;
            aliases[definition.DisplayName] = definition.Code;

            try
            {
                var culture = CultureInfo.GetCultureInfo(definition.Code);
                aliases[culture.EnglishName] = definition.Code;
                aliases[culture.NativeName] = definition.Code;
            }
            catch (CultureNotFoundException)
            {
                // Supported languages should map to valid cultures, but keep initialization resilient.
            }
        }

        return aliases;
    }
}
