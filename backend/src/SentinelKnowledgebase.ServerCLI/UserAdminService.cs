using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SentinelKnowledgebase.Infrastructure.Authentication;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.ServerCLI;

public interface IUserAdminService
{
    Task<IReadOnlyList<UserListItem>> ListUsersAsync(string? role, CancellationToken cancellationToken);
    Task<UserListItem> AddUserAsync(AddUserRequest request, CancellationToken cancellationToken);
    Task<DeleteUserResult> DeleteUserAsync(string email, CancellationToken cancellationToken);
    Task ChangePasswordAsync(string email, string newPassword, CancellationToken cancellationToken);
}

public sealed class UserAdminService : IUserAdminService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAdminService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<UserListItem>> ListUsersAsync(string? role, CancellationToken cancellationToken)
    {
        var normalizedRole = NormalizeRole(role);

        var users = await _dbContext.Users
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);

        var results = new List<UserListItem>(users.Count);
        foreach (var user in users)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            var primaryRole = userRoles.FirstOrDefault() ?? AuthRoles.Member;

            if (normalizedRole != null &&
                !string.Equals(primaryRole, normalizedRole, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            results.Add(new UserListItem(
                user.Id,
                user.Email ?? string.Empty,
                user.DisplayName,
                primaryRole));
        }

        return results;
    }

    public async Task<UserListItem> AddUserAsync(AddUserRequest request, CancellationToken cancellationToken)
    {
        var normalizedRole = NormalizeRole(request.Role)
            ?? throw new InvalidOperationException("Role must be admin or member.");
        var normalizedEmail = NormalizeEmail(request.Email);

        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser != null)
        {
            throw new InvalidOperationException($"A user with email '{normalizedEmail}' already exists.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = normalizedEmail,
            Email = normalizedEmail,
            DisplayName = request.DisplayName.Trim()
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(FormatErrors(createResult.Errors));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, normalizedRole);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            throw new InvalidOperationException(FormatErrors(roleResult.Errors));
        }

        return new UserListItem(user.Id, normalizedEmail, user.DisplayName, normalizedRole);
    }

    public async Task<DeleteUserResult> DeleteUserAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(item => item.NormalizedEmail == normalizedEmail.ToUpperInvariant(), cancellationToken);

        if (user == null)
        {
            return new DeleteUserResult(false, $"User '{normalizedEmail}' was not found.");
        }

        var blockingReasons = new List<string>();

        if (await _dbContext.RawCaptures.AnyAsync(item => item.OwnerUserId == user.Id, cancellationToken))
        {
            blockingReasons.Add("owns captures");
        }

        if (await _dbContext.ProcessedInsights.AnyAsync(item => item.OwnerUserId == user.Id, cancellationToken))
        {
            blockingReasons.Add("owns processed insights");
        }

        if (await _dbContext.Tags.AnyAsync(item => item.OwnerUserId == user.Id, cancellationToken))
        {
            blockingReasons.Add("owns tags");
        }

        if (await _dbContext.UserInvitations.AnyAsync(item => item.InvitedByUserId == user.Id, cancellationToken))
        {
            blockingReasons.Add("created invitations");
        }

        if (await _dbContext.DeviceAuthorizations.AnyAsync(item => item.ApprovedByUserId == user.Id, cancellationToken))
        {
            blockingReasons.Add("approved device authorizations");
        }

        if (blockingReasons.Count > 0)
        {
            var reason = string.Join(", ", blockingReasons);
            return new DeleteUserResult(false, $"Cannot delete '{normalizedEmail}': user {reason}.");
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return new DeleteUserResult(false, FormatErrors(result.Errors));
        }

        return new DeleteUserResult(true, null);
    }

    public async Task ChangePasswordAsync(string email, string newPassword, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await _userManager.FindByEmailAsync(normalizedEmail)
            ?? throw new InvalidOperationException($"User '{normalizedEmail}' was not found.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(FormatErrors(result.Errors));
        }

        await _userManager.UpdateSecurityStampAsync(user);

        var refreshTokens = await _dbContext.RefreshTokens
            .Where(item => item.UserId == user.Id && item.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var refreshToken in refreshTokens)
        {
            refreshToken.RevokedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string? NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return null;
        }

        return role.Trim().ToLowerInvariant() switch
        {
            AuthRoles.Admin => AuthRoles.Admin,
            AuthRoles.Member => AuthRoles.Member,
            _ => null
        };
    }

    private static string FormatErrors(IEnumerable<IdentityError> errors)
    {
        return string.Join(", ", errors.Select(error => error.Description));
    }
}
