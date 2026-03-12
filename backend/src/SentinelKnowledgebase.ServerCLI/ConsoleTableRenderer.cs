namespace SentinelKnowledgebase.ServerCLI;

public static class ConsoleTableRenderer
{
    public static void WriteUsers(TextWriter writer, IReadOnlyList<UserListItem> users)
    {
        if (users.Count == 0)
        {
            writer.WriteLine("No users found.");
            return;
        }

        var idWidth = Math.Max(2, users.Max(user => user.Id.ToString().Length));
        var emailWidth = Math.Max(5, users.Max(user => user.Email.Length));
        var displayNameWidth = Math.Max(11, users.Max(user => user.DisplayName.Length));
        var roleWidth = Math.Max(4, users.Max(user => user.Role.Length));

        WriteRow(writer, idWidth, emailWidth, displayNameWidth, roleWidth, "Id", "Email", "DisplayName", "Role");
        WriteRow(writer, idWidth, emailWidth, displayNameWidth, roleWidth,
            new string('-', idWidth),
            new string('-', emailWidth),
            new string('-', displayNameWidth),
            new string('-', roleWidth));

        foreach (var user in users)
        {
            WriteRow(writer, idWidth, emailWidth, displayNameWidth, roleWidth,
                user.Id.ToString(),
                user.Email,
                user.DisplayName,
                user.Role);
        }
    }

    private static void WriteRow(
        TextWriter writer,
        int idWidth,
        int emailWidth,
        int displayNameWidth,
        int roleWidth,
        string id,
        string email,
        string displayName,
        string role)
    {
        writer.WriteLine(
            $"{id.PadRight(idWidth)}  {email.PadRight(emailWidth)}  {displayName.PadRight(displayNameWidth)}  {role.PadRight(roleWidth)}");
    }
}
