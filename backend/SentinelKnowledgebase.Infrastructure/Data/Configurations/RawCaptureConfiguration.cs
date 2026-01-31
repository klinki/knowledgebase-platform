using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector.EntityFrameworkCore;
using SentinelKnowledgebase.Domain.Entities;

namespace SentinelKnowledgebase.Infrastructure.Data.Configurations;

public class RawCaptureConfiguration : IEntityTypeConfiguration<RawCapture>
{
    public void Configure(EntityTypeBuilder<RawCapture> builder)
    {
        builder.ToTable("raw_captures");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(e => e.SourceUrl)
            .IsRequired()
            .HasMaxLength(2048);
            
        builder.Property(e => e.RawContent)
            .IsRequired();
            
        builder.Property(e => e.Source)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(e => e.CreatedAt)
            .IsRequired();
            
        builder.Property(e => e.ProcessedAt);
            
        builder.HasOne(e => e.ProcessedInsight)
            .WithOne(p => p.RawCapture)
            .HasForeignKey<ProcessedInsight>(p => p.RawCaptureId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(e => e.Tags)
            .WithMany(t => t.Captures)
            .UsingEntity<CaptureTag>(
                j => j.HasOne(ct => ct.Tag).WithMany().HasForeignKey(ct => ct.TagId),
                j => j.HasOne(ct => ct.RawCapture).WithMany().HasForeignKey(ct => ct.RawCaptureId),
                j => j.ToTable("capture_tags")
            );
    }
}
