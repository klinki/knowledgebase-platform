using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SentinelKnowledgebase.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class TwoDimensionalLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LabelCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabelCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabelCategories_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabelValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LabelCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabelValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabelValues_LabelCategories_LabelCategoryId",
                        column: x => x.LabelCategoryId,
                        principalTable: "LabelCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedInsightLabelAssignments",
                columns: table => new
                {
                    ProcessedInsightId = table.Column<Guid>(type: "uuid", nullable: false),
                    LabelCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    LabelValueId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedInsightLabelAssignments", x => new { x.ProcessedInsightId, x.LabelCategoryId });
                    table.ForeignKey(
                        name: "FK_ProcessedInsightLabelAssignments_LabelCategories_LabelCateg~",
                        column: x => x.LabelCategoryId,
                        principalTable: "LabelCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessedInsightLabelAssignments_LabelValues_LabelValueId",
                        column: x => x.LabelValueId,
                        principalTable: "LabelValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcessedInsightLabelAssignments_ProcessedInsights_Processe~",
                        column: x => x.ProcessedInsightId,
                        principalTable: "ProcessedInsights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RawCaptureLabelAssignments",
                columns: table => new
                {
                    RawCaptureId = table.Column<Guid>(type: "uuid", nullable: false),
                    LabelCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    LabelValueId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawCaptureLabelAssignments", x => new { x.RawCaptureId, x.LabelCategoryId });
                    table.ForeignKey(
                        name: "FK_RawCaptureLabelAssignments_LabelCategories_LabelCategoryId",
                        column: x => x.LabelCategoryId,
                        principalTable: "LabelCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RawCaptureLabelAssignments_LabelValues_LabelValueId",
                        column: x => x.LabelValueId,
                        principalTable: "LabelValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RawCaptureLabelAssignments_RawCaptures_RawCaptureId",
                        column: x => x.RawCaptureId,
                        principalTable: "RawCaptures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabelCategories_OwnerUserId_Name",
                table: "LabelCategories",
                columns: new[] { "OwnerUserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabelValues_LabelCategoryId_Value",
                table: "LabelValues",
                columns: new[] { "LabelCategoryId", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedInsightLabelAssignments_LabelCategoryId",
                table: "ProcessedInsightLabelAssignments",
                column: "LabelCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedInsightLabelAssignments_LabelValueId",
                table: "ProcessedInsightLabelAssignments",
                column: "LabelValueId");

            migrationBuilder.CreateIndex(
                name: "IX_RawCaptureLabelAssignments_LabelCategoryId",
                table: "RawCaptureLabelAssignments",
                column: "LabelCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RawCaptureLabelAssignments_LabelValueId",
                table: "RawCaptureLabelAssignments",
                column: "LabelValueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedInsightLabelAssignments");

            migrationBuilder.DropTable(
                name: "RawCaptureLabelAssignments");

            migrationBuilder.DropTable(
                name: "LabelValues");

            migrationBuilder.DropTable(
                name: "LabelCategories");
        }
    }
}
