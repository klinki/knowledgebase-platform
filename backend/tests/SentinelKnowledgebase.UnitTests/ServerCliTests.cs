using AwesomeAssertions;
using NSubstitute;
using SentinelKnowledgebase.ServerCLI;
using Xunit;

namespace SentinelKnowledgebase.UnitTests;

public class ServerCliTests
{
    [Fact]
    public async Task UsersAdd_WithDefaultRoleAndDisplayName_ShouldCreateMemberUser()
    {
        var userAdminService = Substitute.For<IUserAdminService>();
        userAdminService.AddUserAsync(Arg.Any<AddUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var request = callInfo.Arg<AddUserRequest>();
                return new UserListItem(Guid.NewGuid(), request.Email, request.DisplayName, request.Role);
            });

        var output = new StringWriter();
        var error = new StringWriter();
        var cli = new CliApplication(
            userAdminService,
            Substitute.For<IPasswordReader>(),
            output,
            error);

        var exitCode = await cli.InvokeAsync(["users", "add", "new.user@example.com", "--password", "Password123!"]);

        exitCode.Should().Be(0);
        await userAdminService.Received(1).AddUserAsync(
            Arg.Is<AddUserRequest>(request =>
                request.Email == "new.user@example.com" &&
                request.DisplayName == "new.user" &&
                request.Role == "member" &&
                request.Password == "Password123!"),
            Arg.Any<CancellationToken>());
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task UsersChangePassword_WithoutPasswordOption_ShouldPromptAndCallService()
    {
        var userAdminService = Substitute.For<IUserAdminService>();
        var passwordReader = Substitute.For<IPasswordReader>();
        passwordReader.ReadPasswordAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Password123!", "Password123!");

        var cli = new CliApplication(
            userAdminService,
            passwordReader,
            new StringWriter(),
            new StringWriter());

        var exitCode = await cli.InvokeAsync(["users", "change-password", "member@example.com"]);

        exitCode.Should().Be(0);
        await userAdminService.Received(1)
            .ChangePasswordAsync("member@example.com", "Password123!", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UsersAdd_WithMismatchedPromptPasswords_ShouldReturnFailure()
    {
        var userAdminService = Substitute.For<IUserAdminService>();
        var passwordReader = Substitute.For<IPasswordReader>();
        passwordReader.ReadPasswordAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Password123!", "Different123!");

        var output = new StringWriter();
        var error = new StringWriter();
        var cli = new CliApplication(userAdminService, passwordReader, output, error);

        var exitCode = await cli.InvokeAsync(["users", "add", "new.user@example.com"]);

        exitCode.Should().Be(1);
        error.ToString().Should().Contain("Passwords do not match.");
        await userAdminService.DidNotReceiveWithAnyArgs()
            .AddUserAsync(default!, default);
    }

    [Fact]
    public async Task HelpCommand_ShouldReturnHelpText()
    {
        var cli = new CliApplication(
            Substitute.For<IUserAdminService>(),
            Substitute.For<IPasswordReader>(),
            new StringWriter(),
            new StringWriter());

        var exitCode = await cli.InvokeAsync(["help", "users"]);

        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task VersionCommand_ShouldWriteAssemblyVersion()
    {
        var output = new StringWriter();
        var cli = new CliApplication(
            Substitute.For<IUserAdminService>(),
            Substitute.For<IPasswordReader>(),
            output,
            new StringWriter());

        var exitCode = await cli.InvokeAsync(["version"]);

        exitCode.Should().Be(0);
        output.ToString().Trim().Should().NotBeNullOrWhiteSpace();
    }
}
