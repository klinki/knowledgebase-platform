using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector.EntityFrameworkCore;
using SentinelKnowledgebase.Domain.Entities;

namespace SentinelKnowledgebase.Infrastructure.Data.Configurations;

public class ProcessedInsightConfiguration : IEntityTypeConfiguration<ProcessedInsight>
{
    public void Configure(EntityTypeBuilder<ProcessedInsight> builder)
    {
        builder.ToTable("processed_insights");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(e => e.RawCaptureId)
            .IsRequired();
            
        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(e => e.Summary)
            .IsRequired();
            
        builder.Property(e => e.CleanContent)
            .IsRequired();
            
        builder.Property(e => e.Embedding)
            .HasColumnType("vector(1536)")
            .IsRequired();
            
        builder.Property(e => e.ProcessedAt)
            .IsRequired();
            
        builder.HasOne(e => e.RawCapture)
            .WithOne(rc => rc.ProcessedInsight)
            .HasForeignKey<ProcessedInsight>(e => e.RawCaptureId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasIndex(e => e.Embedding)
            .HasMethod("ivfflat")
            .HasOperators("vector_l2_ops");
    }
}
