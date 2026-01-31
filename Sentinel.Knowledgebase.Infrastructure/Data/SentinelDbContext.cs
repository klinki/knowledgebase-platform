using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pgvector;
using Sentinel.Knowledgebase.Domain.Entities;

namespace Sentinel.Knowledgebase.Infrastructure.Data;

public class SentinelDbContext : DbContext
{
    public SentinelDbContext(DbContextOptions<SentinelDbContext> options) : base(options)
    {
    }

    public DbSet<RawCapture> RawCaptures { get; set; }
    public DbSet<ProcessedInsight> ProcessedInsights { get; set; }
    public DbSet<SearchHistory> SearchHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure RawCapture
        modelBuilder.Entity<RawCapture>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SourceUrl).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Author).HasMaxLength(200);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            
            // Configure Metadata as JSON
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb");
            
            // Configure relationship
            entity.HasOne(e => e.ProcessedInsight)
                .WithOne(pi => pi.RawCapture)
                .HasForeignKey<ProcessedInsight>(pi => pi.RawCaptureId);
        });

        // Configure ProcessedInsight
        modelBuilder.Entity<ProcessedInsight>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Summary).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.KeyPoints).IsRequired();
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ProcessingError).HasMaxLength(1000);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            
            // Configure Tags as JSON array
            entity.Property(e => e.Tags)
                .HasColumnType("jsonb");
            
            // Configure Embedding vector
            entity.Property(e => e.Embedding)
                .HasColumnType("vector(1536)");
        });

        // Configure SearchHistory
        modelBuilder.Entity<SearchHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Query).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.SearchType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            
            // Configure SearchParameters as JSON
            entity.Property(e => e.SearchParameters)
                .HasColumnType("jsonb");
        });

        // Configure indexes for better performance
        modelBuilder.Entity<RawCapture>()
            .HasIndex(e => e.Status);
        
        modelBuilder.Entity<RawCapture>()
            .HasIndex(e => e.SourceUrl);
        
        modelBuilder.Entity<ProcessedInsight>()
            .HasIndex(e => e.RawCaptureId)
            .IsUnique();
        
        modelBuilder.Entity<ProcessedInsight>()
            .HasIndex(e => e.Category);
        
        modelBuilder.Entity<SearchHistory>()
            .HasIndex(e => e.SearchType);
        
        modelBuilder.Entity<SearchHistory>()
            .HasIndex(e => e.CreatedAt);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Default configuration for development
            optionsBuilder.UseNpgsql("Host=localhost;Database=sentinel_knowledgebase;Username=sentinel;Password=sentinel123");
        }
    }
}
