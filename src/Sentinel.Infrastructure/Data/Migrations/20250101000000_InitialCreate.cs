using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Sentinel.Infrastructure.Data.Migrations;

[DbContext(typeof(SentinelDbContext))]
[Migration("20250101000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:PostgresExtension:vector", ",,");

        migrationBuilder.CreateTable(
            name: "raw_captures",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SourceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                RawText = table.Column<string>(type: "text", nullable: false),
                Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                AuthorHandle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_raw_captures", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "tags",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tags", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "processed_insights",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RawCaptureId = table.Column<Guid>(type: "uuid", nullable: false),
                Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Insight = table.Column<string>(type: "text", nullable: false),
                Sentiment = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                CleanText = table.Column<string>(type: "text", nullable: false),
                Embedding = table.Column<Vector>(type: "vector(1536)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_processed_insights", x => x.Id);
                table.ForeignKey(
                    name: "FK_processed_insights_raw_captures_RawCaptureId",
                    column: x => x.RawCaptureId,
                    principalTable: "raw_captures",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "processed_insight_tags",
            columns: table => new
            {
                ProcessedInsightId = table.Column<Guid>(type: "uuid", nullable: false),
                TagId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_processed_insight_tags", x => new { x.ProcessedInsightId, x.TagId });
                table.ForeignKey(
                    name: "FK_processed_insight_tags_processed_insights_ProcessedInsightId",
                    column: x => x.ProcessedInsightId,
                    principalTable: "processed_insights",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_processed_insight_tags_tags_TagId",
                    column: x => x.TagId,
                    principalTable: "tags",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_processed_insights_RawCaptureId",
            table: "processed_insights",
            column: "RawCaptureId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_processed_insight_tags_TagId",
            table: "processed_insight_tags",
            column: "TagId");

        migrationBuilder.CreateIndex(
            name: "IX_raw_captures_SourceId",
            table: "raw_captures",
            column: "SourceId");

        migrationBuilder.CreateIndex(
            name: "IX_tags_Name",
            table: "tags",
            column: "Name",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "processed_insight_tags");

        migrationBuilder.DropTable(
            name: "processed_insights");

        migrationBuilder.DropTable(
            name: "tags");

        migrationBuilder.DropTable(
            name: "raw_captures");
    }
}
