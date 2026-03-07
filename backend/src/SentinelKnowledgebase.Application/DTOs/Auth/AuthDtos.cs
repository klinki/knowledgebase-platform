namespace SentinelKnowledgebase.Application.DTOs.Auth;

public sealed class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class AuthUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class InvitationRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class InvitationResponseDto
{
    public Guid InvitationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class AcceptInvitationRequestDto
{
    public string Token { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class ResetPasswordRequestDto
{
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class DeviceStartRequestDto
{
    public string DeviceName { get; set; } = string.Empty;
}

public sealed class DeviceStartResponseDto
{
    public string DeviceCode { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public string VerificationUrl { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public int IntervalSeconds { get; set; }
}

public sealed class DeviceApproveRequestDto
{
    public string UserCode { get; set; } = string.Empty;
}

public sealed class DevicePollRequestDto
{
    public string DeviceCode { get; set; } = string.Empty;
}

public sealed class TokenRefreshRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class TokenRevokeRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class TokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public AuthUserDto User { get; set; } = new();
}
