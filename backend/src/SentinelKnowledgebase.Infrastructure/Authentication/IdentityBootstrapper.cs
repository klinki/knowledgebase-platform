using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace SentinelKnowledgebase.Infrastructure.Authentication;

public sealed class IdentityBootstrapper
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuthOptions _options;

    public IdentityBootstrapper(
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        IOptions<AuthOptions> options)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _options = options.Value;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var role in new[] { AuthRoles.Admin, AuthRoles.Member })
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        if (string.IsNullOrWhiteSpace(_options.BootstrapAdminEmail) ||
            string.IsNullOrWhiteSpace(_options.BootstrapAdminPassword))
        {
            return;
        }

        var email = _options.BootstrapAdminEmail.Trim().ToLowerInvariant();
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            if (!await _userManager.IsInRoleAsync(existingUser, AuthRoles.Admin))
            {
                await _userManager.AddToRoleAsync(existingUser, AuthRoles.Admin);
            }

            return;
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            DisplayName = _options.BootstrapAdminDisplayName
        };

        var result = await _userManager.CreateAsync(user, _options.BootstrapAdminPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to create bootstrap admin user: {errors}");
        }

        await _userManager.AddToRoleAsync(user, AuthRoles.Admin);
    }
}
