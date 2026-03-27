using System.CommandLine;
using System.Reflection;

namespace SentinelKnowledgebase.ImportCLI;

internal sealed class CliApplication
{
    private readonly ITwitterLikesImportService _importService;
    private readonly TextWriter _output;
    private readonly TextWriter _error;
    private RootCommand? _rootCommand;

    public CliApplication(
        ITwitterLikesImportService importService,
        TextWriter output,
        TextWriter error)
    {
        _importService = importService;
        _output = output;
        _error = error;
    }

    public RootCommand BuildRootCommand()
    {
        if (_rootCommand != null)
        {
            return _rootCommand;
        }

        var rootCommand = new RootCommand("Sentinel archive import CLI");
        var twitterCommand = new Command("twitter", "Import Twitter archive data");
        twitterCommand.Add(BuildLikesCommand());

        rootCommand.Add(twitterCommand);
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

    private Command BuildLikesCommand()
    {
        var inputOption = new Option<string>("--input")
        {
            Description = "Path to the Twitter archive root directory or .zip file.",
            Required = true
        };
        var apiUrlOption = new Option<string>("--api-url")
        {
            Description = "Sentinel API base URL, for example https://localhost:5001.",
            Required = true
        };

        var command = new Command("likes", "Import liked tweets from a Twitter archive");
        command.Add(inputOption);
        command.Add(apiUrlOption);
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            try
            {
                var options = new TwitterLikesImportOptions(
                    parseResult.GetValue(inputOption)!,
                    parseResult.GetValue(apiUrlOption)!);
                var summary = await _importService.ImportAsync(options, cancellationToken);
                WriteSummary(summary);
                return 0;
            }
            catch (Exception exception)
            {
                _error.WriteLine(exception.Message);
                return 1;
            }
        });

        return command;
    }

    private Command BuildVersionCommand()
    {
        var command = new Command("version", "Show version information");
        command.SetAction(_ =>
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

    private void WriteSummary(TwitterLikesImportSummary summary)
    {
        _output.WriteLine("Import complete.");
        _output.WriteLine($"Total likes read: {summary.TotalLikesRead}");
        _output.WriteLine($"Duplicates skipped: {summary.DuplicatesSkipped}");
        _output.WriteLine($"Successfully submitted captures: {summary.SuccessfulImports}");
        _output.WriteLine($"Failed submissions: {summary.FailedSubmissions}");
        _output.WriteLine($"Malformed/skipped records: {summary.MalformedRecords}");
    }
}
