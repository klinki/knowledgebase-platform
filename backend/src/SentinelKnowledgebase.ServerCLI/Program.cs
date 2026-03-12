using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SentinelKnowledgebase.Infrastructure;

namespace SentinelKnowledgebase.ServerCLI;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.ClearProviders();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddScoped<IUserAdminService, UserAdminService>();
        builder.Services.AddSingleton<IPasswordReader, ConsolePasswordReader>();

        using var host = builder.Build();
        using var scope = host.Services.CreateScope();

        var cli = new CliApplication(
            scope.ServiceProvider.GetRequiredService<IUserAdminService>(),
            scope.ServiceProvider.GetRequiredService<IPasswordReader>(),
            Console.Out,
            Console.Error);

        return await cli.InvokeAsync(args);
    }
}
