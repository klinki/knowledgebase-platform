using Microsoft.EntityFrameworkCore;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;

namespace SentinelKnowledgebase.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    public DbSet<RawCapture> RawCaptures { get; set; }
    public DbSet<ProcessedInsight> ProcessedInsights { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<EmbeddingVector> EmbeddingVectors { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<RawCapture>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SourceUrl).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.RawContent).IsRequired();
            entity.Property(e => e.Status).HasDefaultValue(CaptureStatus.Pending);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasMany(e => e.Tags)
                .WithMany(t => t.RawCaptures);
            
            entity.HasOne(e => e.ProcessedInsight)
                .WithOne(p => p.RawCapture)
                .HasForeignKey<ProcessedInsight>(p => p.RawCaptureId);
        });
        
        modelBuilder.Entity<ProcessedInsight>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Summary).IsRequired();
            entity.Property(e => e.ProcessedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasMany(e => e.Tags)
                .WithMany(t => t.ProcessedInsights);
            
            entity.HasOne(e => e.EmbeddingVector)
                .WithOne()
                .HasForeignKey<EmbeddingVector>(e => e.ProcessedInsightId);
        });
        
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => e.Name).IsUnique();
        });
        
        modelBuilder.Entity<EmbeddingVector>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProcessedInsightId).IsRequired();
            entity.Property(e => e.Vector).HasColumnType("vector(1536)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
