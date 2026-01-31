using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sentinel.Infrastructure.Data;

public sealed class SentinelDbContextFactory : IDesignTimeDbContextFactory<SentinelDbContext>
{
    public SentinelDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SENTINEL_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=sentinel;Username=sentinel;Password=sentinelpassword";

        var optionsBuilder = new DbContextOptionsBuilder<SentinelDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql => npgsql.UseVector());

        return new SentinelDbContext(optionsBuilder.Options);
    }
}
