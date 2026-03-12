namespace SentinelKnowledgebase.ServerCLI;

public sealed record UserListItem(Guid Id, string Email, string DisplayName, string Role);

public sealed record AddUserRequest(string Email, string DisplayName, string Role, string Password);

public sealed record DeleteUserResult(bool Succeeded, string? FailureReason);
