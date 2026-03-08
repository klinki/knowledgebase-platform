using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SentinelKnowledgebase.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class EntityOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"ProcessedInsightTag\";");
            migrationBuilder.Sql("DELETE FROM \"RawCaptureTag\";");
            migrationBuilder.Sql("DELETE FROM \"EmbeddingVectors\";");
            migrationBuilder.Sql("DELETE FROM \"ProcessedInsights\";");
            migrationBuilder.Sql("DELETE FROM \"RawCaptures\";");
            migrationBuilder.Sql("DELETE FROM \"Tags\";");

            migrationBuilder.DropIndex(
                name: "IX_Tags_Name",
                table: "Tags");

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "Tags",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "RawCaptures",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "ProcessedInsights",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Tags_OwnerUserId_Name",
                table: "Tags",
                columns: new[] { "OwnerUserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RawCaptures_OwnerUserId_CreatedAt",
                table: "RawCaptures",
                columns: new[] { "OwnerUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedInsights_OwnerUserId_ProcessedAt",
                table: "ProcessedInsights",
                columns: new[] { "OwnerUserId", "ProcessedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessedInsights_AspNetUsers_OwnerUserId",
                table: "ProcessedInsights",
                column: "OwnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RawCaptures_AspNetUsers_OwnerUserId",
                table: "RawCaptures",
                column: "OwnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_AspNetUsers_OwnerUserId",
                table: "Tags",
                column: "OwnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcessedInsights_AspNetUsers_OwnerUserId",
                table: "ProcessedInsights");

            migrationBuilder.DropForeignKey(
                name: "FK_RawCaptures_AspNetUsers_OwnerUserId",
                table: "RawCaptures");

            migrationBuilder.DropForeignKey(
                name: "FK_Tags_AspNetUsers_OwnerUserId",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Tags_OwnerUserId_Name",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_RawCaptures_OwnerUserId_CreatedAt",
                table: "RawCaptures");

            migrationBuilder.DropIndex(
                name: "IX_ProcessedInsights_OwnerUserId_ProcessedAt",
                table: "ProcessedInsights");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "RawCaptures");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "ProcessedInsights");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);
        }
    }
}
