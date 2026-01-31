using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NpgsqlTypes;
using Sentinel.Domain.Constants;

#nullable disable

namespace Sentinel.Infrastructure.Data.Migrations;

[DbContext(typeof(SentinelDbContext))]
public partial class SentinelDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "9.0.4");
        modelBuilder.HasAnnotation("Relational:MaxIdentifierLength", 63);

        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity("Sentinel.Domain.Entities.RawCapture", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<string>("AuthorHandle")
                .HasMaxLength(200)
                .HasColumnType("character varying(200)");

            b.Property<DateTimeOffset>("CapturedAt")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            b.Property<string>("ErrorMessage")
                .HasMaxLength(2000)
                .HasColumnType("character varying(2000)");

            b.Property<DateTimeOffset?>("ProcessedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("RawText")
                .IsRequired()
                .HasColumnType("text");

            b.Property<string>("Source")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("character varying(32)");

            b.Property<string>("SourceId")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)");

            b.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("character varying(32)");

            b.Property<string>("Url")
                .HasMaxLength(2048)
                .HasColumnType("character varying(2048)");

            b.HasKey("Id");

            b.HasIndex("SourceId");

            b.ToTable("raw_captures");
        });

        modelBuilder.Entity("Sentinel.Domain.Entities.ProcessedInsight", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<string>("CleanText")
                .IsRequired()
                .HasColumnType("text");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            b.Property<Vector>("Embedding")
                .HasColumnType("vector(1536)");

            b.Property<string>("Insight")
                .IsRequired()
                .HasColumnType("text");

            b.Property<Guid>("RawCaptureId")
                .HasColumnType("uuid");

            b.Property<string>("Sentiment")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("character varying(32)");

            b.Property<string>("Summary")
                .IsRequired()
                .HasMaxLength(2000)
                .HasColumnType("character varying(2000)");

            b.HasKey("Id");

            b.HasIndex("RawCaptureId")
                .IsUnique();

            b.ToTable("processed_insights");
        });

        modelBuilder.Entity("Sentinel.Domain.Entities.Tag", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.HasKey("Id");

            b.HasIndex("Name")
                .IsUnique();

            b.ToTable("tags");
        });

        modelBuilder.Entity("Sentinel.Domain.Entities.ProcessedInsightTag", b =>
        {
            b.Property<Guid>("ProcessedInsightId")
                .HasColumnType("uuid");

            b.Property<Guid>("TagId")
                .HasColumnType("uuid");

            b.HasKey("ProcessedInsightId", "TagId");

            b.HasIndex("TagId");

            b.ToTable("processed_insight_tags");
        });

        modelBuilder.Entity("Sentinel.Domain.Entities.ProcessedInsight", b =>
        {
            b.HasOne("Sentinel.Domain.Entities.RawCapture", "RawCapture")
                .WithOne("ProcessedInsight")
                .HasForeignKey("Sentinel.Domain.Entities.ProcessedInsight", "RawCaptureId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("RawCapture");
            b.Navigation("Tags");
        });

        modelBuilder.Entity("Sentinel.Domain.Entities.ProcessedInsightTag", b =>
        {
            b.HasOne("Sentinel.Domain.Entities.ProcessedInsight", "ProcessedInsight")
                .WithMany("Tags")
                .HasForeignKey("ProcessedInsightId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("Sentinel.Domain.Entities.Tag", "Tag")
                .WithMany("Insights")
                .HasForeignKey("TagId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("ProcessedInsight");
            b.Navigation("Tag");
        });

        modelBuilder.Entity("Sentinel.Domain.Entities.RawCapture", b =>
        {
            b.Navigation("ProcessedInsight");
        });

        modelBuilder.Entity("Sentinel.Domain.Entities.Tag", b =>
        {
            b.Navigation("Insights");
        });
    }
}
