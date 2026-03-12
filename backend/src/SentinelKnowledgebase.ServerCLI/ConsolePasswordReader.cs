namespace SentinelKnowledgebase.ServerCLI;

public interface IPasswordReader
{
    Task<string> ReadPasswordAsync(string prompt, CancellationToken cancellationToken);
}

public sealed class ConsolePasswordReader : IPasswordReader
{
    public Task<string> ReadPasswordAsync(string prompt, CancellationToken cancellationToken)
    {
        if (Console.IsInputRedirected)
        {
            throw new InvalidOperationException("Password must be provided with --password when console input is redirected.");
        }

        Console.Write(prompt);
        var buffer = new List<char>();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Count == 0)
                {
                    continue;
                }

                buffer.RemoveAt(buffer.Count - 1);
                continue;
            }

            if (char.IsControl(key.KeyChar))
            {
                continue;
            }

            buffer.Add(key.KeyChar);
        }

        return Task.FromResult(new string(buffer.ToArray()));
    }
}
