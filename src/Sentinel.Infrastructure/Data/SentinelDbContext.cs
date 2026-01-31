using Microsoft.EntityFrameworkCore;
using Sentinel.Domain.Constants;
using Sentinel.Domain.Entities;

namespace Sentinel.Infrastructure.Data;

public sealed class SentinelDbContext : DbContext
{
    public SentinelDbContext(DbContextOptions<SentinelDbContext> options)
        : base(options)
    {
    }

    public DbSet<RawCapture> RawCaptures => Set<RawCapture>();
    public DbSet<ProcessedInsight> ProcessedInsights => Set<ProcessedInsight>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ProcessedInsightTag> ProcessedInsightTags => Set<ProcessedInsightTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<RawCapture>(entity =>
        {
            entity.ToTable("raw_captures");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SourceId).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Source).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.RawText).IsRequired();
            entity.Property(x => x.Url).HasMaxLength(2048);
            entity.Property(x => x.AuthorHandle).HasMaxLength(200);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(x => x.CapturedAt).HasDefaultValueSql("now()");
            entity.HasIndex(x => x.SourceId);
        });

        modelBuilder.Entity<ProcessedInsight>(entity =>
        {
            entity.ToTable("processed_insights");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Summary).IsRequired().HasMaxLength(2000);
            entity.Property(x => x.Insight).IsRequired();
            entity.Property(x => x.CleanText).IsRequired();
            entity.Property(x => x.Sentiment).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Embedding)
                .HasColumnType($"vector({EmbeddingConstants.Dimensions})");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(x => x.RawCapture)
                .WithOne(x => x.ProcessedInsight)
                .HasForeignKey<ProcessedInsight>(x => x.RawCaptureId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.RawCaptureId).IsUnique();
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("tags");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<ProcessedInsightTag>(entity =>
        {
            entity.ToTable("processed_insight_tags");
            entity.HasKey(x => new { x.ProcessedInsightId, x.TagId });
            entity.HasOne(x => x.ProcessedInsight)
                .WithMany(x => x.Tags)
                .HasForeignKey(x => x.ProcessedInsightId);
            entity.HasOne(x => x.Tag)
                .WithMany(x => x.Insights)
                .HasForeignKey(x => x.TagId);
            entity.HasIndex(x => x.TagId);
        });
    }
}
