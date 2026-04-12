namespace SentinelKnowledgebase.ImportCLI;

internal interface IImportReporter
{
    void WriteInfo(string message);
    void WriteWarning(string message);
    void WriteError(string message);
}

internal sealed class ConsoleImportReporter : IImportReporter
{
    private readonly TextWriter _output;
    private readonly TextWriter _error;

    public ConsoleImportReporter(TextWriter output, TextWriter error)
    {
        _output = output;
        _error = error;
    }

    public void WriteInfo(string message)
    {
        _output.WriteLine(message);
    }

    public void WriteWarning(string message)
    {
        _output.WriteLine(message);
    }

    public void WriteError(string message)
    {
        _error.WriteLine(message);
    }
}
