using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Authentication;
using SentinelKnowledgebase.Infrastructure.Data;
using SentinelKnowledgebase.ServerCLI;
using Xunit;

namespace SentinelKnowledgebase.IntegrationTests;

[Collection("IntegrationTests")]
public class ServerCliUserAdminServiceTests
{
    private readonly IntegrationTestFixture _fixture;

    public ServerCliUserAdminServiceTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddUserAsync_WithMemberRole_ShouldCreateUser()
    {
        using var scope = _fixture.CreateScope();
        var service = CreateService(scope);

        var user = await service.AddUserAsync(
            new AddUserRequest("member.cli@sentinel.test", "CLI Member", AuthRoles.Member, "Password123!"),
            CancellationToken.None);

        user.Email.Should().Be("member.cli@sentinel.test");
        user.Role.Should().Be(AuthRoles.Member);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var createdUser = await userManager.FindByEmailAsync("member.cli@sentinel.test");
        createdUser.Should().NotBeNull();
        (await userManager.GetRolesAsync(createdUser!)).Should().Contain(AuthRoles.Member);
    }

    [Fact]
    public async Task AddUserAsync_WithAdminRole_ShouldAssignAdminRole()
    {
        using var scope = _fixture.CreateScope();
        var service = CreateService(scope);

        var user = await service.AddUserAsync(
            new AddUserRequest("admin.cli@sentinel.test", "CLI Admin", AuthRoles.Admin, "Password123!"),
            CancellationToken.None);

        user.Role.Should().Be(AuthRoles.Admin);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var createdUser = await userManager.FindByEmailAsync("admin.cli@sentinel.test");
        createdUser.Should().NotBeNull();
        (await userManager.GetRolesAsync(createdUser!)).Should().Contain(AuthRoles.Admin);
    }

    [Fact]
    public async Task AddUserAsync_WithDuplicateEmail_ShouldThrow()
    {
        using var scope = _fixture.CreateScope();
        var service = CreateService(scope);
        var request = new AddUserRequest("duplicate.cli@sentinel.test", "Duplicate", AuthRoles.Member, "Password123!");

        await service.AddUserAsync(request, CancellationToken.None);

        var action = async () => await service.AddUserAsync(request, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task ListUsersAsync_ShouldReturnUsersWithRoles()
    {
        using var scope = _fixture.CreateScope();
        var service = CreateService(scope);

        await service.AddUserAsync(
            new AddUserRequest("list-member@sentinel.test", "List Member", AuthRoles.Member, "Password123!"),
            CancellationToken.None);
        await service.AddUserAsync(
            new AddUserRequest("list-admin@sentinel.test", "List Admin", AuthRoles.Admin, "Password123!"),
            CancellationToken.None);

        var allUsers = await service.ListUsersAsync(null, CancellationToken.None);
        var admins = await service.ListUsersAsync(AuthRoles.Admin, CancellationToken.None);

        allUsers.Should().Contain(user => user.Email == "list-member@sentinel.test" && user.Role == AuthRoles.Member);
        allUsers.Should().Contain(user => user.Email == "list-admin@sentinel.test" && user.Role == AuthRoles.Admin);
        admins.Should().ContainSingle(user => user.Email == "list-admin@sentinel.test");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldRevokeRefreshTokensAndAllowNewPassword()
    {
        using var scope = _fixture.CreateScope();
        var service = CreateService(scope);
        var user = await service.AddUserAsync(
            new AddUserRequest("password.cli@sentinel.test", "Password User", AuthRoles.Member, "OldPassword123!"),
            CancellationToken.None);

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = "refresh-token-hash",
                TokenName = "CLI Session",
                Scope = "offline_access",
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
            });

            await Task.CompletedTask;
        });

        await service.ChangePasswordAsync("password.cli@sentinel.test", "NewPassword123!", CancellationToken.None);

        using var oldClient = _fixture.CreateClient();
        var oldLoginResponse = await oldClient.PostAsJsonAsync("/api/auth/login", new { Email = "password.cli@sentinel.test", Password = "OldPassword123!" });
        oldLoginResponse.IsSuccessStatusCode.Should().BeFalse();

        using var newClient = _fixture.CreateClient();
        var newLoginResponse = await newClient.PostAsJsonAsync("/api/auth/login", new { Email = "password.cli@sentinel.test", Password = "NewPassword123!" });
        newLoginResponse.IsSuccessStatusCode.Should().BeTrue();

        using var verifyScope = _fixture.CreateScope();
        var dbContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var refreshToken = await dbContext.RefreshTokens.SingleAsync(item => item.UserId == user.Id);
        refreshToken.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteUserAsync_WithOwnedData_ShouldBlockDeletion()
    {
        using var scope = _fixture.CreateScope();
        var service = CreateService(scope);
        var user = await service.AddUserAsync(
            new AddUserRequest("owns-data.cli@sentinel.test", "Owns Data", AuthRoles.Member, "Password123!"),
            CancellationToken.None);

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.RawCaptures.Add(new RawCapture
            {
                Id = Guid.NewGuid(),
                OwnerUserId = user.Id,
                SourceUrl = "https://example.com/owned-data",
                ContentType = ContentType.Article,
                RawContent = "Owned content"
            });

            await Task.CompletedTask;
        });

        var result = await service.DeleteUserAsync("owns-data.cli@sentinel.test", CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.FailureReason.Should().Contain("owns captures");
    }

    [Fact]
    public async Task DeleteUserAsync_WithInvitationReference_ShouldBlockDeletion()
    {
        using var scope = _fixture.CreateScope();
        var service = CreateService(scope);
        var user = await service.AddUserAsync(
            new AddUserRequest("inviter.cli@sentinel.test", "Inviter", AuthRoles.Admin, "Password123!"),
            CancellationToken.None);

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.UserInvitations.Add(new UserInvitation
            {
                Id = Guid.NewGuid(),
                Email = "invited@sentinel.test",
                DisplayName = "Invited User",
                Role = AuthRoles.Member,
                TokenHash = "invitation-token-hash",
                InvitedByUserId = user.Id,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
            });

            await Task.CompletedTask;
        });

        var result = await service.DeleteUserAsync("inviter.cli@sentinel.test", CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.FailureReason.Should().Contain("created invitations");
    }

    [Fact]
    public async Task DeleteUserAsync_WithApprovedDeviceAuthorization_ShouldBlockDeletion()
    {
        using var scope = _fixture.CreateScope();
        var service = CreateService(scope);
        var user = await service.AddUserAsync(
            new AddUserRequest("approver.cli@sentinel.test", "Approver", AuthRoles.Member, "Password123!"),
            CancellationToken.None);

        await _fixture.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.DeviceAuthorizations.Add(new DeviceAuthorization
            {
                Id = Guid.NewGuid(),
                DeviceCode = "device-code",
                UserCode = "USER-CODE",
                DeviceName = "CLI Test Device",
                ApprovedByUserId = user.Id,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
                ApprovedAt = DateTimeOffset.UtcNow
            });

            await Task.CompletedTask;
        });

        var result = await service.DeleteUserAsync("approver.cli@sentinel.test", CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.FailureReason.Should().Contain("approved device authorizations");
    }

    [Fact]
    public async Task DeleteUserAsync_WithoutReferences_ShouldDeleteUser()
    {
        using var scope = _fixture.CreateScope();
        var service = CreateService(scope);
        await service.AddUserAsync(
            new AddUserRequest("delete.cli@sentinel.test", "Delete User", AuthRoles.Member, "Password123!"),
            CancellationToken.None);

        var result = await service.DeleteUserAsync("delete.cli@sentinel.test", CancellationToken.None);

        result.Succeeded.Should().BeTrue();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var deletedUser = await userManager.FindByEmailAsync("delete.cli@sentinel.test");
        deletedUser.Should().BeNull();
    }

    private static UserAdminService CreateService(IServiceScope scope)
    {
        return new UserAdminService(
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>());
    }
}
