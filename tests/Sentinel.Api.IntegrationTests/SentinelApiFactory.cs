using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Sentinel.Api.IntegrationTests;

public sealed class SentinelApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public SentinelApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:SentinelDb"] = _connectionString
            };

            config.AddInMemoryCollection(settings);
        });
    }
}
