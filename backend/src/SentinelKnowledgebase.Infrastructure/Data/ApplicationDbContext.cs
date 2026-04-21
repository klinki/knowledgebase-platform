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
    public DbSet<LabelCategory> LabelCategories { get; set; }
    public DbSet<LabelValue> LabelValues { get; set; }
    public DbSet<RawCaptureLabelAssignment> RawCaptureLabelAssignments { get; set; }
    public DbSet<ProcessedInsightLabelAssignment> ProcessedInsightLabelAssignments { get; set; }
    public DbSet<EmbeddingVector> EmbeddingVectors { get; set; }
    public DbSet<InsightCluster> InsightClusters { get; set; }
    public DbSet<InsightClusterMembership> InsightClusterMemberships { get; set; }
    public DbSet<CaptureProcessingControl> CaptureProcessingControls { get; set; }
    public DbSet<AssistantChatSession> AssistantChatSessions { get; set; }
    public DbSet<AssistantChatMessage> AssistantChatMessages { get; set; }
    public DbSet<AssistantChatResultSet> AssistantChatResultSets { get; set; }
    public DbSet<AssistantChatPendingAction> AssistantChatPendingActions { get; set; }
    public DbSet<UserInvitation> UserInvitations { get; set; }
    public DbSet<DeviceAuthorization> DeviceAuthorizations { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<UserPreservedLanguage> UserPreservedLanguages { get; set; }
    public DbSet<TelegramChatLink> TelegramChatLinks { get; set; }
    public DbSet<TelegramLinkCode> TelegramLinkCodes { get; set; }
    public DbSet<TelegramIngestionState> TelegramIngestionStates { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.HasPostgresExtension("vector"); // Ensure the extension is enabled in DB

        modelBuilder.Entity<RawCapture>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerUserId).IsRequired();
            entity.Property(e => e.SourceUrl).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.RawContent).IsRequired();
            entity.Property(e => e.Status).HasDefaultValue(CaptureStatus.Pending);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.DeletedAt);
            entity.HasIndex(e => new { e.OwnerUserId, e.IsDeleted, e.CreatedAt });
            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(e => e.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.Tags)
                .WithMany(t => t.RawCaptures);

            entity.HasMany(e => e.LabelAssignments)
                .WithOne(a => a.RawCapture)
                .HasForeignKey(a => a.RawCaptureId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.ProcessedInsight)
                .WithOne(p => p.RawCapture)
                .HasForeignKey<ProcessedInsight>(p => p.RawCaptureId);
        });
        
        modelBuilder.Entity<ProcessedInsight>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerUserId).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Summary).IsRequired();
            entity.Property(e => e.ProcessedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.DeletedAt);
            entity.HasIndex(e => new { e.OwnerUserId, e.IsDeleted, e.ProcessedAt });
            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(e => e.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.Tags)
                .WithMany(t => t.ProcessedInsights);

            entity.HasMany(e => e.LabelAssignments)
                .WithOne(a => a.ProcessedInsight)
                .HasForeignKey(a => a.ProcessedInsightId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.EmbeddingVector)
                .WithOne(e => e.ProcessedInsight)
                .HasForeignKey<EmbeddingVector>(e => e.ProcessedInsightId);

            entity.HasOne(e => e.ClusterMembership)
                .WithOne(e => e.ProcessedInsight)
                .HasForeignKey<InsightClusterMembership>(e => e.ProcessedInsightId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerUserId).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(e => e.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.OwnerUserId, e.Name }).IsUnique();
        });

        modelBuilder.Entity<LabelCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerUserId).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(e => e.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.OwnerUserId, e.Name }).IsUnique();
        });

        modelBuilder.Entity<LabelValue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Value).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.LabelCategory)
                .WithMany(c => c.Values)
                .HasForeignKey(e => e.LabelCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.LabelCategoryId, e.Value }).IsUnique();
        });

        modelBuilder.Entity<RawCaptureLabelAssignment>(entity =>
        {
            entity.HasKey(e => new { e.RawCaptureId, e.LabelCategoryId });

            entity.HasOne(e => e.LabelCategory)
                .WithMany()
                .HasForeignKey(e => e.LabelCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.LabelValue)
                .WithMany(v => v.RawCaptureAssignments)
                .HasForeignKey(e => e.LabelValueId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.LabelValueId);
        });

        modelBuilder.Entity<ProcessedInsightLabelAssignment>(entity =>
        {
            entity.HasKey(e => new { e.ProcessedInsightId, e.LabelCategoryId });

            entity.HasOne(e => e.LabelCategory)
                .WithMany()
                .HasForeignKey(e => e.LabelCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.LabelValue)
                .WithMany(v => v.ProcessedInsightAssignments)
                .HasForeignKey(e => e.LabelValueId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.LabelValueId);
        });
        
        modelBuilder.Entity<EmbeddingVector>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProcessedInsightId).IsRequired();
            entity.Property(e => e.Vector).HasColumnType("vector(1536)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<InsightCluster>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerUserId).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(60).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(160);
            entity.Property(e => e.KeywordsJson).IsRequired();
            entity.Property(e => e.LastComputedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(e => e.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.OwnerUserId, e.UpdatedAt });

            entity.HasOne(e => e.RepresentativeProcessedInsight)
                .WithMany()
                .HasForeignKey(e => e.RepresentativeProcessedInsightId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<InsightClusterMembership>(entity =>
        {
            entity.HasKey(e => e.ProcessedInsightId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => new { e.InsightClusterId, e.Rank });

            entity.HasOne(e => e.InsightCluster)
                .WithMany(e => e.Memberships)
                .HasForeignKey(e => e.InsightClusterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CaptureProcessingControl>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.IsPaused).HasDefaultValue(false);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(e => e.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasData(new CaptureProcessingControl
            {
                Id = CaptureProcessingControl.SingletonId,
                IsPaused = false,
                ChangedAt = null,
                ChangedByUserId = null
            });
        });


        modelBuilder.Entity<TelegramChatLink>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerUserId).IsRequired();
            entity.Property(e => e.LinkedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => new { e.OwnerUserId, e.UnlinkedAt });
            entity.HasIndex(e => new { e.TelegramChatId, e.UnlinkedAt });

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(e => e.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TelegramLinkCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerUserId).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => new { e.OwnerUserId, e.ExpiresAt });

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(e => e.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TelegramIngestionState>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.LastProcessedUpdateId).HasDefaultValue(0L);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasData(new TelegramIngestionState
            {
                Id = TelegramIngestionState.SingletonId,
                LastProcessedUpdateId = 0L,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        });

        modelBuilder.Entity<AssistantChatSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerUserId).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.OwnerUserId).IsUnique();

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(e => e.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssistantChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => new { e.SessionId, e.CreatedAt });

            entity.HasOne(e => e.Session)
                .WithMany(session => session.Messages)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssistantChatResultSet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QueryType).HasMaxLength(80).IsRequired();
            entity.Property(e => e.Summary).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CaptureIdsJson).IsRequired();
            entity.Property(e => e.PreviewJson).IsRequired();
            entity.Property(e => e.CriteriaJson).IsRequired().HasDefaultValue("{}");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => new { e.SessionId, e.CreatedAt });
            entity.HasIndex(e => new { e.OwnerUserId, e.CreatedAt });

            entity.HasOne(e => e.Session)
                .WithMany(session => session.ResultSets)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(e => e.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssistantChatPendingAction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CaptureIdsJson).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => new { e.SessionId, e.CreatedAt });
            entity.HasIndex(e => new { e.OwnerUserId, e.Status });

            entity.HasOne(e => e.Session)
                .WithMany(session => session.PendingActions)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(e => e.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.DefaultLanguageCode).HasMaxLength(16);
            entity.HasMany(e => e.PreservedLanguages)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserPreservedLanguage>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LanguageCode });
            entity.Property(e => e.LanguageCode).HasMaxLength(16).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
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
