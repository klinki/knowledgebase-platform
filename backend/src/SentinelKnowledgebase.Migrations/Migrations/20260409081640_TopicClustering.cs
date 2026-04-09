using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SentinelKnowledgebase.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class TopicClustering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InsightClusters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Description = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    KeywordsJson = table.Column<string>(type: "text", nullable: false),
                    MemberCount = table.Column<int>(type: "integer", nullable: false),
                    RepresentativeProcessedInsightId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastComputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsightClusters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsightClusters_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InsightClusters_ProcessedInsights_RepresentativeProcessedIn~",
                        column: x => x.RepresentativeProcessedInsightId,
                        principalTable: "ProcessedInsights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InsightClusterMemberships",
                columns: table => new
                {
                    ProcessedInsightId = table.Column<Guid>(type: "uuid", nullable: false),
                    InsightClusterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    SimilarityToCentroid = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsightClusterMemberships", x => x.ProcessedInsightId);
                    table.ForeignKey(
                        name: "FK_InsightClusterMemberships_InsightClusters_InsightClusterId",
                        column: x => x.InsightClusterId,
                        principalTable: "InsightClusters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InsightClusterMemberships_ProcessedInsights_ProcessedInsigh~",
                        column: x => x.ProcessedInsightId,
                        principalTable: "ProcessedInsights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InsightClusterMemberships_InsightClusterId_Rank",
                table: "InsightClusterMemberships",
                columns: new[] { "InsightClusterId", "Rank" });

            migrationBuilder.CreateIndex(
                name: "IX_InsightClusters_OwnerUserId_UpdatedAt",
                table: "InsightClusters",
                columns: new[] { "OwnerUserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InsightClusters_RepresentativeProcessedInsightId",
                table: "InsightClusters",
                column: "RepresentativeProcessedInsightId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InsightClusterMemberships");

            migrationBuilder.DropTable(
                name: "InsightClusters");
        }
    }
}
