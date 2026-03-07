using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SentinelKnowledgebase.Domain.Entities;
using SentinelKnowledgebase.Domain.Enums;
using SentinelKnowledgebase.Infrastructure.Authentication;

namespace SentinelKnowledgebase.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    public DbSet<RawCapture> RawCaptures { get; set; }
    public DbSet<ProcessedInsight> ProcessedInsights { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<EmbeddingVector> EmbeddingVectors { get; set; }
    public DbSet<UserInvitation> UserInvitations { get; set; }
    public DbSet<DeviceAuthorization> DeviceAuthorizations { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.HasPostgresExtension("vector"); // Ensure the extension is enabled in DB

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
                .WithOne(e => e.ProcessedInsight)
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

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<UserInvitation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(320).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(50).IsRequired();
            entity.Property(e => e.TokenHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasOne(e => e.InvitedByUser)
                .WithMany()
                .HasForeignKey(e => e.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DeviceAuthorization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DeviceCode).HasMaxLength(128).IsRequired();
            entity.Property(e => e.UserCode).HasMaxLength(32).IsRequired();
            entity.Property(e => e.DeviceName).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.DeviceCode).IsUnique();
            entity.HasIndex(e => e.UserCode).IsUnique();
            entity.HasOne(e => e.ApprovedByUser)
                .WithMany()
                .HasForeignKey(e => e.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(e => e.TokenName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Scope).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.DeviceAuthorization)
                .WithMany()
                .HasForeignKey(e => e.DeviceAuthorizationId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
