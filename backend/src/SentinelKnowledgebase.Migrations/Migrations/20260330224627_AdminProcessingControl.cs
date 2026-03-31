using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SentinelKnowledgebase.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AdminProcessingControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CaptureProcessingControls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    IsPaused = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaptureProcessingControls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaptureProcessingControls_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "CaptureProcessingControls",
                columns: new[] { "Id", "ChangedAt", "ChangedByUserId" },
                values: new object[] { 1, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_CaptureProcessingControls_ChangedByUserId",
                table: "CaptureProcessingControls",
                column: "ChangedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaptureProcessingControls");
        }
    }
}
