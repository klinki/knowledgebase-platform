using System.CommandLine;
using System.Reflection;
using SentinelKnowledgebase.Infrastructure.Authentication;

namespace SentinelKnowledgebase.ServerCLI;

public sealed class CliApplication
{
    private readonly IUserAdminService _userAdminService;
    private readonly IPasswordReader _passwordReader;
    private readonly TextWriter _output;
    private readonly TextWriter _error;
    private RootCommand? _rootCommand;

    public CliApplication(
        IUserAdminService userAdminService,
        IPasswordReader passwordReader,
        TextWriter output,
        TextWriter error)
    {
        _userAdminService = userAdminService;
        _passwordReader = passwordReader;
        _output = output;
        _error = error;
    }

    public RootCommand BuildRootCommand()
    {
        if (_rootCommand != null)
        {
            return _rootCommand;
        }

        var rootCommand = new RootCommand("Sentinel server administration CLI");

        var usersCommand = new Command("users", "Manage Sentinel users");
        usersCommand.Add(BuildListCommand());
        usersCommand.Add(BuildAddCommand());
        usersCommand.Add(BuildDeleteCommand());
        usersCommand.Add(BuildChangePasswordCommand());

        rootCommand.Add(usersCommand);
        rootCommand.Add(BuildHelpCommand());
        rootCommand.Add(BuildVersionCommand());

        _rootCommand = rootCommand;
        return rootCommand;
    }

    public Task<int> InvokeAsync(string[] args)
    {
        var parseResult = BuildRootCommand().Parse(args);
        return parseResult.InvokeAsync(new InvocationConfiguration
        {
            Output = _output,
            Error = _error
        });
    }

    private Command BuildListCommand()
    {
        var roleOption = new Option<string?>("--role")
        {
            Description = "Filter by role (admin or member)"
        };
        var command = new Command("list", "List users");
        command.Add(roleOption);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            try
            {
                var role = parseResult.GetValue(roleOption);
                ValidateRole(role);
                var users = await _userAdminService.ListUsersAsync(role, cancellationToken);
                ConsoleTableRenderer.WriteUsers(_output, users);
                return 0;
            }
            catch (Exception exception)
            {
                return HandleFailure(exception);
            }
        });

        return command;
    }

    private Command BuildAddCommand()
    {
        var emailArgument = new Argument<string>("email")
        {
            Description = "Email address for the new user"
        };
        var displayNameOption = new Option<string?>("--display-name")
        {
            Description = "Display name for the new user"
        };
        var roleOption = new Option<string>("--role")
        {
            Description = "Role for the new user",
            DefaultValueFactory = _ => AuthRoles.Member
        };
        var passwordOption = new Option<string?>("--password")
        {
            Description = "Password for the new user"
        };

        var command = new Command("add", "Create a new user");
        command.Add(emailArgument);
        command.Add(displayNameOption);
        command.Add(roleOption);
        command.Add(passwordOption);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            try
            {
                var email = parseResult.GetValue(emailArgument);
                var role = parseResult.GetValue(roleOption) ?? AuthRoles.Member;
                ValidateRole(role);
                var displayName = parseResult.GetValue(displayNameOption);
                var password = await ResolvePasswordAsync(
                    parseResult.GetValue(passwordOption),
                    "Password: ",
                    "Confirm password: ",
                    cancellationToken);

                var user = await _userAdminService.AddUserAsync(
                    new AddUserRequest(
                        email!,
                        ResolveDisplayName(displayName, email!),
                        role,
                        password),
                    cancellationToken);

                _output.WriteLine($"Created user '{user.Email}' with role '{user.Role}'.");
                return 0;
            }
            catch (Exception exception)
            {
                return HandleFailure(exception);
            }
        });

        return command;
    }

    private Command BuildDeleteCommand()
    {
        var emailArgument = new Argument<string>("email")
        {
            Description = "Email address of the user to delete"
        };

        var command = new Command("delete", "Delete a user");
        command.Add(emailArgument);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            try
            {
                var email = parseResult.GetValue(emailArgument);
                var result = await _userAdminService.DeleteUserAsync(email!, cancellationToken);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(result.FailureReason ?? $"Failed to delete '{email}'.");
                }

                _output.WriteLine($"Deleted user '{email!.Trim().ToLowerInvariant()}'.");
                return 0;
            }
            catch (Exception exception)
            {
                return HandleFailure(exception);
            }
        });

        return command;
    }

    private Command BuildChangePasswordCommand()
    {
        var emailArgument = new Argument<string>("email")
        {
            Description = "Email address of the user"
        };
        var passwordOption = new Option<string?>("--password")
        {
            Description = "New password"
        };

        var command = new Command("change-password", "Change a user's password");
        command.Add(emailArgument);
        command.Add(passwordOption);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            try
            {
                var email = parseResult.GetValue(emailArgument);
                var password = await ResolvePasswordAsync(
                    parseResult.GetValue(passwordOption),
                    "New password: ",
                    "Confirm new password: ",
                    cancellationToken);

                await _userAdminService.ChangePasswordAsync(email!, password, cancellationToken);
                _output.WriteLine($"Changed password for '{email!.Trim().ToLowerInvariant()}'.");
                return 0;
            }
            catch (Exception exception)
            {
                return HandleFailure(exception);
            }
        });

        return command;
    }

    private Command BuildHelpCommand()
    {
        var pathArgument = new Argument<List<string>>("command")
        {
            Arity = ArgumentArity.ZeroOrMore,
            Description = "Optional command path"
        };

        var command = new Command("help", "Show help for a command");
        command.Add(pathArgument);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var path = parseResult.GetValue(pathArgument) ?? [];
            var args = path.Concat(["--help"]).ToArray();
            var helpParseResult = BuildRootCommand().Parse(args);
            return await helpParseResult.InvokeAsync(new InvocationConfiguration
            {
                Output = _output,
                Error = _error
            }, cancellationToken);
        });

        return command;
    }

    private Command BuildVersionCommand()
    {
        var command = new Command("version", "Show version information");
        command.SetAction(parseResult =>
        {
            var version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? "unknown";

            _output.WriteLine(version);
            return Task.FromResult(0);
        });

        return command;
    }

    private async Task<string> ResolvePasswordAsync(
        string? password,
        string prompt,
        string confirmationPrompt,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(password))
        {
            return password;
        }

        var first = await _passwordReader.ReadPasswordAsync(prompt, cancellationToken);
        if (string.IsNullOrWhiteSpace(first))
        {
            throw new InvalidOperationException("Password cannot be empty.");
        }

        var second = await _passwordReader.ReadPasswordAsync(confirmationPrompt, cancellationToken);
        if (!string.Equals(first, second, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Passwords do not match.");
        }

        return first;
    }

    private static string ResolveDisplayName(string? displayName, string email)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName.Trim();
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var separatorIndex = normalizedEmail.IndexOf('@');
        return separatorIndex > 0 ? normalizedEmail[..separatorIndex] : normalizedEmail;
    }

    private static void ValidateRole(string? role)
    {
        var normalizedRole = role?.Trim().ToLowerInvariant();
        if (normalizedRole != AuthRoles.Admin && normalizedRole != AuthRoles.Member)
        {
            throw new InvalidOperationException("Role must be admin or member.");
        }
    }

    private int HandleFailure(Exception exception)
    {
        _error.WriteLine(exception.Message);
        return 1;
    }
}
