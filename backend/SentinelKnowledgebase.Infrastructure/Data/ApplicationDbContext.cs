using Microsoft.EntityFrameworkCore;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Infrastructure.Data.Configurations;

namespace SentinelKnowledgebase.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<RawCapture> RawCaptures { get; set; } = null!;
    public DbSet<ProcessedInsight> ProcessedInsights { get; set; } = null!;
    public DbSet<Tag> Tags { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new RawCaptureConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessedInsightConfiguration());
        modelBuilder.ApplyConfiguration(new TagConfiguration());
    }
}
