using System.Text.Json;

namespace SentinelKnowledgebase.ImportCLI;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        using var httpClient = new HttpClient();
        var jsonOptions = JsonDefaults.Create();
        var reporter = new ConsoleImportReporter(Console.Out, Console.Error);
        var tokenCache = new FileTokenCache(jsonOptions);
        var authClient = new DeviceAuthClient(httpClient, tokenCache, reporter, jsonOptions, TimeProvider.System);
        var captureClient = new SentinelCaptureClient(httpClient, authClient, jsonOptions);
        var archiveResolver = new ArchiveInputResolver();
        var importSource = new TwitterLikesImportSource();
        var mapper = new TwitterLikeCaptureMapper(jsonOptions);
        var importService = new TwitterLikesImportService(
            archiveResolver,
            importSource,
            mapper,
            captureClient,
            reporter,
            TimeProvider.System);

        var cli = new CliApplication(importService, Console.Out, Console.Error);
        return await cli.InvokeAsync(args);
    }
}
