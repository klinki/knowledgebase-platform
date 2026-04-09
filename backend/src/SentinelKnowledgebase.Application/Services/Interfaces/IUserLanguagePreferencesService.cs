namespace SentinelKnowledgebase.Application.Services.Interfaces;

public sealed class UserLanguagePreferencesSnapshot
{
    public string DefaultLanguageCode { get; init; } = string.Empty;
    public IReadOnlyList<string> PreservedLanguageCodes { get; init; } = [];
}

public interface IUserLanguagePreferencesService
{
    Task<UserLanguagePreferencesSnapshot> GetAsync(
        Guid userId,
        string? acceptLanguageHeader = null,
        CancellationToken cancellationToken = default);

    Task<UserLanguagePreferencesSnapshot> UpdateAsync(
        Guid userId,
        string defaultLanguageCode,
        IReadOnlyCollection<string>? preservedLanguageCodes,
        CancellationToken cancellationToken = default);
}
