using System.Security.Claims;
using System.Security.Cryptography;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using SentinelKnowledgebase.Application.DTOs.Auth;
using SentinelKnowledgebase.Api.Extensions;
using SentinelKnowledgebase.Infrastructure.Authentication;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TokenService _tokenService;
    private readonly AuthOptions _authOptions;

    public AuthController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TokenService tokenService,
        IOptions<AuthOptions> authOptions)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _authOptions = authOptions.Value;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthUserDto>> Login([FromBody] LoginRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return Unauthorized();
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName ?? email,
            request.Password,
            isPersistent: true,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Unauthorized();
        }

        return Ok(await BuildAuthUserAsync(user));
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthUserDto>> Me()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(await BuildAuthUserAsync(user));
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("invitations")]
    [ProducesResponseType(typeof(InvitationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvitationResponseDto>> CreateInvitation([FromBody] InvitationRequestDto request)
    {
        var normalizedRole = NormalizeRole(request.Role);
        if (normalizedRole == null)
        {
            return BadRequest("Role must be admin or member.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Email is required.");
        }

        if (await _userManager.FindByEmailAsync(email) != null)
        {
            return BadRequest("A user with this email already exists.");
        }

        var invitedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (invitedByUserId == null)
        {
            return Unauthorized();
        }

        var invitationToken = CreateOpaqueToken();
        var invitation = new UserInvitation
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? email.Split('@')[0] : request.DisplayName.Trim(),
            Role = normalizedRole,
            TokenHash = _tokenService.HashOpaqueToken(invitationToken),
            InvitedByUserId = Guid.Parse(invitedByUserId),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        _dbContext.UserInvitations.Add(invitation);
        await _dbContext.SaveChangesAsync();

        var response = new InvitationResponseDto
        {
            InvitationId = invitation.Id,
            Email = invitation.Email,
            Role = invitation.Role,
            Token = invitationToken,
            ExpiresAt = invitation.ExpiresAt
        };

        return CreatedAtAction(nameof(CreateInvitation), response);
    }

    [HttpPost("invitations/accept")]
    [ProducesResponseType(typeof(AuthUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthUserDto>> AcceptInvitation([FromBody] AcceptInvitationRequestDto request)
    {
        var tokenHash = _tokenService.HashOpaqueToken(request.Token.Trim());
        var invitation = await _dbContext.UserInvitations
            .FirstOrDefaultAsync(item => item.TokenHash == tokenHash);

        if (invitation == null || invitation.AcceptedAt.HasValue || invitation.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return BadRequest("Invitation is invalid or expired.");
        }

        if (await _userManager.FindByEmailAsync(invitation.Email) != null)
        {
            return BadRequest("A user with this email already exists.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = invitation.Email,
            Email = invitation.Email,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? invitation.DisplayName : request.DisplayName.Trim()
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(createResult.Errors);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, invitation.Role);
        if (!roleResult.Succeeded)
        {
            return BadRequest(roleResult.Errors);
        }

        invitation.AcceptedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Created(string.Empty, await BuildAuthUserAsync(user));
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("users/{id:guid}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromRoute] Guid id, [FromBody] ResetPasswordRequestDto request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return NotFound();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        await _userManager.UpdateSecurityStampAsync(user);
        await RevokeAllRefreshTokensForUserAsync(user.Id);

        return NoContent();
    }

    [HttpPost("device/start")]
    [ProducesResponseType(typeof(DeviceStartResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DeviceStartResponseDto>> StartDeviceAuthorization([FromBody] DeviceStartRequestDto request)
    {
        var deviceAuthorization = new DeviceAuthorization
        {
            Id = Guid.NewGuid(),
            DeviceCode = CreateOpaqueToken(),
            UserCode = CreateUserCode(),
            DeviceName = string.IsNullOrWhiteSpace(request.DeviceName) ? "Sentinel Browser Extension" : request.DeviceName.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_authOptions.DeviceCodeLifetimeMinutes)
        };

        _dbContext.DeviceAuthorizations.Add(deviceAuthorization);
        await _dbContext.SaveChangesAsync();

        return Ok(new DeviceStartResponseDto
        {
            DeviceCode = deviceAuthorization.DeviceCode,
            UserCode = deviceAuthorization.UserCode,
            VerificationUrl = $"{_authOptions.FrontendUrl.TrimEnd('/')}/login?userCode={Uri.EscapeDataString(deviceAuthorization.UserCode)}",
            ExpiresAt = deviceAuthorization.ExpiresAt,
            IntervalSeconds = _authOptions.DevicePollingIntervalSeconds
        });
    }

    [Authorize]
    [HttpPost("device/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApproveDeviceAuthorization([FromBody] DeviceApproveRequestDto request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized();
        }

        var authorization = await _dbContext.DeviceAuthorizations
            .FirstOrDefaultAsync(item => item.UserCode == request.UserCode.Trim().ToUpperInvariant());

        if (authorization == null || authorization.ExpiresAt <= DateTimeOffset.UtcNow || authorization.CompletedAt.HasValue)
        {
            return BadRequest("Device authorization is invalid or expired.");
        }

        authorization.ApprovedByUserId = user.Id;
        authorization.ApprovedAt = DateTimeOffset.UtcNow;
        authorization.Denied = false;
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("device/poll")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PollDeviceAuthorization([FromBody] DevicePollRequestDto request)
    {
        var authorization = await _dbContext.DeviceAuthorizations
            .Include(item => item.ApprovedByUser)
            .FirstOrDefaultAsync(item => item.DeviceCode == request.DeviceCode.Trim());

        if (authorization == null)
        {
            return BadRequest(new { error = "invalid_device_code" });
        }

        if (authorization.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return BadRequest(new { error = "expired_token" });
        }

        if (authorization.Denied)
        {
            return BadRequest(new { error = "access_denied" });
        }

        if (authorization.ApprovedByUser == null || !authorization.ApprovedAt.HasValue)
        {
            return Accepted(new { status = "pending" });
        }

        if (authorization.CompletedAt.HasValue)
        {
            return BadRequest(new { error = "authorization_already_completed" });
        }

        var user = authorization.ApprovedByUser;
        var role = await GetPrimaryRoleAsync(user);
        var tokenPair = _tokenService.CreateTokenPair(user, role, authorization.Id, "capture:write search:read offline_access");

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DeviceAuthorizationId = authorization.Id,
            TokenHash = _tokenService.HashOpaqueToken(tokenPair.RefreshToken),
            TokenName = authorization.DeviceName,
            Scope = "capture:write search:read offline_access",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_authOptions.RefreshTokenLifetimeDays)
        });

        authorization.CompletedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(new TokenResponseDto
        {
            AccessToken = tokenPair.AccessToken,
            RefreshToken = tokenPair.RefreshToken,
            ExpiresAt = tokenPair.ExpiresAt,
            User = await BuildAuthUserAsync(user)
        });
    }

    [HttpPost("token/refresh")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponseDto>> RefreshToken([FromBody] TokenRefreshRequestDto request)
    {
        var hashedToken = _tokenService.HashOpaqueToken(request.RefreshToken.Trim());
        var refreshToken = await _dbContext.RefreshTokens
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.TokenHash == hashedToken);

        if (refreshToken == null || refreshToken.IsRevoked || refreshToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return Unauthorized();
        }

        var role = await GetPrimaryRoleAsync(refreshToken.User);
        var tokenPair = _tokenService.CreateTokenPair(refreshToken.User, role, refreshToken.DeviceAuthorizationId, refreshToken.Scope);

        refreshToken.RevokedAt = DateTimeOffset.UtcNow;
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = refreshToken.UserId,
            DeviceAuthorizationId = refreshToken.DeviceAuthorizationId,
            TokenHash = _tokenService.HashOpaqueToken(tokenPair.RefreshToken),
            TokenName = refreshToken.TokenName,
            Scope = refreshToken.Scope,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_authOptions.RefreshTokenLifetimeDays)
        });

        await _dbContext.SaveChangesAsync();

        return Ok(new TokenResponseDto
        {
            AccessToken = tokenPair.AccessToken,
            RefreshToken = tokenPair.RefreshToken,
            ExpiresAt = tokenPair.ExpiresAt,
            User = await BuildAuthUserAsync(refreshToken.User)
        });
    }

    [Authorize]
    [HttpPost("token/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeToken([FromBody] TokenRevokeRequestDto request)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var hashedToken = _tokenService.HashOpaqueToken(request.RefreshToken.Trim());
        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(item => item.TokenHash == hashedToken && item.UserId == userId);

        if (refreshToken != null && !refreshToken.IsRevoked)
        {
            refreshToken.RevokedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        return NoContent();
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        return await _userManager.GetUserAsync(User);
    }

    private async Task<AuthUserDto> BuildAuthUserAsync(ApplicationUser user)
    {
        var role = await GetPrimaryRoleAsync(user);
        return new AuthUserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Role = role
        };
    }

    private async Task<string> GetPrimaryRoleAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return roles.FirstOrDefault() ?? AuthRoles.Member;
    }

    private async Task RevokeAllRefreshTokensForUserAsync(Guid userId)
    {
        var refreshTokens = await _dbContext.RefreshTokens
            .Where(item => item.UserId == userId && item.RevokedAt == null)
            .ToListAsync();

        foreach (var refreshToken in refreshTokens)
        {
            refreshToken.RevokedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
    }

    private static string? NormalizeRole(string role)
    {
        return role.Trim().ToLowerInvariant() switch
        {
            AuthRoles.Admin => AuthRoles.Admin,
            AuthRoles.Member => AuthRoles.Member,
            _ => null
        };
    }

    private static string CreateOpaqueToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string CreateUserCode()
    {
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        return $"{Convert.ToHexString(bytes[..2])}-{Convert.ToHexString(bytes[2..]).ToUpperInvariant()}";
    }
}
