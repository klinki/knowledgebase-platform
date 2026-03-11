using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pgvector.EntityFrameworkCore;
using SentinelKnowledgebase.Infrastructure.Data;

namespace SentinelKnowledgebase.Migrations;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string DesignTimeConnectionString =
        "Host=localhost;Port=5432;Database=sentinel_knowledgebase;Username=postgres;Password=postgres";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? DesignTimeConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(DesignTimeDbContextFactory).Assembly.GetName().Name);
            npgsqlOptions.UseVector();
        });

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
