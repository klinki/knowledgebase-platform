using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SentinelKnowledgebase.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteCapturesAndInsights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RawCaptures_OwnerUserId_CreatedAt",
                table: "RawCaptures");

            migrationBuilder.DropIndex(
                name: "IX_ProcessedInsights_OwnerUserId_ProcessedAt",
                table: "ProcessedInsights");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RawCaptures",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RawCaptures",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ProcessedInsights",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProcessedInsights",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_RawCaptures_OwnerUserId_IsDeleted_CreatedAt",
                table: "RawCaptures",
                columns: new[] { "OwnerUserId", "IsDeleted", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedInsights_OwnerUserId_IsDeleted_ProcessedAt",
                table: "ProcessedInsights",
                columns: new[] { "OwnerUserId", "IsDeleted", "ProcessedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RawCaptures_OwnerUserId_IsDeleted_CreatedAt",
                table: "RawCaptures");

            migrationBuilder.DropIndex(
                name: "IX_ProcessedInsights_OwnerUserId_IsDeleted_ProcessedAt",
                table: "ProcessedInsights");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RawCaptures");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RawCaptures");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ProcessedInsights");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProcessedInsights");

            migrationBuilder.CreateIndex(
                name: "IX_RawCaptures_OwnerUserId_CreatedAt",
                table: "RawCaptures",
                columns: new[] { "OwnerUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedInsights_OwnerUserId_ProcessedAt",
                table: "ProcessedInsights",
                columns: new[] { "OwnerUserId", "ProcessedAt" });
        }
    }
}
