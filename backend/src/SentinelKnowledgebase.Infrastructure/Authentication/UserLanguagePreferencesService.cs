using Microsoft.EntityFrameworkCore;

using SentinelKnowledgebase.Domain.Localization;
using SentinelKnowledgebase.Domain.Services;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Infrastructure.Authentication;

public sealed class UserLanguagePreferencesService : IUserLanguagePreferencesService
{
    private readonly ApplicationDbContext _dbContext;

    public UserLanguagePreferencesService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserLanguagePreferencesSnapshot> GetAsync(
        Guid userId,
        string? acceptLanguageHeader = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException($"User '{userId}' was not found.");

        var shouldPersistDefault = false;
        var defaultLanguageCode = LanguageCatalog.NormalizeSupportedLanguageCode(user.DefaultLanguageCode);
        if (defaultLanguageCode == null)
        {
            defaultLanguageCode = LanguageCatalog.ResolveSupportedLanguageCode(acceptLanguageHeader);
            user.DefaultLanguageCode = defaultLanguageCode;
            shouldPersistDefault = true;
        }

        var preservedLanguageCodes = await _dbContext.UserPreservedLanguages
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => item.LanguageCode)
            .ToListAsync(cancellationToken);

        if (shouldPersistDefault)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new UserLanguagePreferencesSnapshot
        {
            DefaultLanguageCode = defaultLanguageCode,
            PreservedLanguageCodes = preservedLanguageCodes
                .Select(LanguageCatalog.NormalizeSupportedLanguageCode)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code!)
                .Where(code => !string.Equals(code, defaultLanguageCode, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    public async Task<UserLanguagePreferencesSnapshot> UpdateAsync(
        Guid userId,
        string defaultLanguageCode,
        IReadOnlyCollection<string>? preservedLanguageCodes,
        CancellationToken cancellationToken = default)
    {
        var normalizedDefaultLanguageCode = LanguageCatalog.NormalizeSupportedLanguageCode(defaultLanguageCode);
        if (normalizedDefaultLanguageCode == null)
        {
            throw new ArgumentException("Default language must be a supported base language code.", nameof(defaultLanguageCode));
        }

        var normalizedPreservedLanguageCodes = NormalizePreservedLanguages(preservedLanguageCodes, normalizedDefaultLanguageCode);

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException($"User '{userId}' was not found.");

        user.DefaultLanguageCode = normalizedDefaultLanguageCode;

        var existing = await _dbContext.UserPreservedLanguages
            .Where(item => item.UserId == userId)
            .ToListAsync(cancellationToken);

        var desired = new HashSet<string>(normalizedPreservedLanguageCodes, StringComparer.OrdinalIgnoreCase);

        foreach (var record in existing.Where(record => !desired.Contains(record.LanguageCode)).ToList())
        {
            _dbContext.UserPreservedLanguages.Remove(record);
        }

        var existingCodes = existing
            .Select(item => item.LanguageCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var missingCode in desired.Where(code => !existingCodes.Contains(code)))
        {
            _dbContext.UserPreservedLanguages.Add(new UserPreservedLanguage
            {
                UserId = userId,
                LanguageCode = missingCode
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UserLanguagePreferencesSnapshot
        {
            DefaultLanguageCode = normalizedDefaultLanguageCode,
            PreservedLanguageCodes = normalizedPreservedLanguageCodes
        };
    }

    private static IReadOnlyList<string> NormalizePreservedLanguages(
        IReadOnlyCollection<string>? preservedLanguageCodes,
        string defaultLanguageCode)
    {
        if (preservedLanguageCodes == null || preservedLanguageCodes.Count == 0)
        {
            return [];
        }

        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var preservedLanguageCode in preservedLanguageCodes)
        {
            var normalizedCode = LanguageCatalog.NormalizeSupportedLanguageCode(preservedLanguageCode);
            if (normalizedCode == null)
            {
                throw new ArgumentException(
                    $"Preserved language '{preservedLanguageCode}' is not a supported base language code.",
                    nameof(preservedLanguageCodes));
            }

            if (string.Equals(normalizedCode, defaultLanguageCode, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            normalized.Add(normalizedCode);
        }

        return normalized
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
