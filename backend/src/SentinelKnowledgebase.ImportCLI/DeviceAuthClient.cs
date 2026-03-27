using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SentinelKnowledgebase.Application.DTOs.Auth;

namespace SentinelKnowledgebase.ImportCLI;

internal sealed class DeviceAuthClient
{
    private static readonly TimeSpan AccessTokenRefreshBuffer = TimeSpan.FromSeconds(30);
    private readonly HttpClient _httpClient;
    private readonly ITokenCache _tokenCache;
    private readonly IImportReporter _reporter;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly TimeProvider _timeProvider;

    public DeviceAuthClient(
        HttpClient httpClient,
        ITokenCache tokenCache,
        IImportReporter reporter,
        JsonSerializerOptions jsonOptions,
        TimeProvider timeProvider)
    {
        _httpClient = httpClient;
        _tokenCache = tokenCache;
        _reporter = reporter;
        _jsonOptions = jsonOptions;
        _timeProvider = timeProvider;
    }

    public async Task<string> GetAccessTokenAsync(string apiUrl, CancellationToken cancellationToken)
    {
        var session = await _tokenCache.GetAsync(apiUrl, cancellationToken);
        if (session != null && !IsExpiring(session.ExpiresAt))
        {
            return session.AccessToken;
        }

        if (session != null && !string.IsNullOrWhiteSpace(session.RefreshToken))
        {
            var refreshed = await TryRefreshAsync(apiUrl, session.RefreshToken, cancellationToken);
            if (refreshed != null)
            {
                await _tokenCache.SaveAsync(ToCachedSession(apiUrl, refreshed), cancellationToken);
                return refreshed.AccessToken;
            }

            await _tokenCache.ClearAsync(apiUrl, cancellationToken);
            _reporter.WriteWarning("Stored session could not be refreshed. Starting a new device sign-in.");
        }

        var authorizedSession = await RunDeviceAuthorizationAsync(apiUrl, cancellationToken);
        await _tokenCache.SaveAsync(authorizedSession, cancellationToken);
        return authorizedSession.AccessToken;
    }

    public Task InvalidateSessionAsync(string apiUrl, CancellationToken cancellationToken)
    {
        return _tokenCache.ClearAsync(apiUrl, cancellationToken);
    }

    private async Task<TokenResponseDto?> TryRefreshAsync(
        string apiUrl,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            $"{ApiUrlNormalizer.Normalize(apiUrl)}/api/auth/token/refresh",
            new TokenRefreshRequestDto { RefreshToken = refreshToken },
            _jsonOptions,
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Failed to refresh Sentinel session: {(int)response.StatusCode} {errorText}");
        }

        return await response.Content.ReadFromJsonAsync<TokenResponseDto>(_jsonOptions, cancellationToken);
    }

    private async Task<CachedAuthSession> RunDeviceAuthorizationAsync(string apiUrl, CancellationToken cancellationToken)
    {
        _reporter.WriteInfo("Starting Sentinel device sign-in...");

        using var startResponse = await _httpClient.PostAsJsonAsync(
            $"{ApiUrlNormalizer.Normalize(apiUrl)}/api/auth/device/start",
            new DeviceStartRequestDto { DeviceName = "Sentinel Import CLI" },
            _jsonOptions,
            cancellationToken);

        if (!startResponse.IsSuccessStatusCode)
        {
            var errorText = await startResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Failed to start device sign-in: {(int)startResponse.StatusCode} {errorText}");
        }

        var startPayload = await startResponse.Content.ReadFromJsonAsync<DeviceStartResponseDto>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Device sign-in start response was empty.");

        _reporter.WriteInfo($"Open this URL to approve access: {startPayload.VerificationUrl}");
        _reporter.WriteInfo($"User code: {startPayload.UserCode}");
        _reporter.WriteInfo("Waiting for approval...");

        var expiresAt = startPayload.ExpiresAt;
        while (_timeProvider.GetUtcNow() < expiresAt)
        {
            await Task.Delay(TimeSpan.FromSeconds(startPayload.IntervalSeconds), cancellationToken);

            using var pollResponse = await _httpClient.PostAsJsonAsync(
                $"{ApiUrlNormalizer.Normalize(apiUrl)}/api/auth/device/poll",
                new DevicePollRequestDto { DeviceCode = startPayload.DeviceCode },
                _jsonOptions,
                cancellationToken);

            if (pollResponse.StatusCode == HttpStatusCode.Accepted)
            {
                continue;
            }

            if (!pollResponse.IsSuccessStatusCode)
            {
                var errorText = await pollResponse.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    $"Device sign-in failed: {(int)pollResponse.StatusCode} {errorText}");
            }

            var tokenResponse = await pollResponse.Content.ReadFromJsonAsync<TokenResponseDto>(_jsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Device sign-in response did not contain tokens.");

            _reporter.WriteInfo($"Signed in as {tokenResponse.User.DisplayName}.");
            return ToCachedSession(apiUrl, tokenResponse);
        }

        throw new InvalidOperationException("Device sign-in timed out before approval completed.");
    }

    private bool IsExpiring(DateTimeOffset expiresAt)
    {
        return expiresAt <= _timeProvider.GetUtcNow().Add(AccessTokenRefreshBuffer);
    }

    private static CachedAuthSession ToCachedSession(string apiUrl, TokenResponseDto response)
    {
        return new CachedAuthSession
        {
            ApiUrl = ApiUrlNormalizer.Normalize(apiUrl),
            AccessToken = response.AccessToken,
            RefreshToken = response.RefreshToken,
            ExpiresAt = response.ExpiresAt,
            User = response.User
        };
    }
}
