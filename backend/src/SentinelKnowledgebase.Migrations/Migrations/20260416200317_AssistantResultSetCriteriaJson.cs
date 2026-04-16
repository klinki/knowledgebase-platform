using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SentinelKnowledgebase.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AssistantResultSetCriteriaJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CriteriaJson",
                table: "AssistantChatResultSets",
                type: "text",
                nullable: false,
                defaultValue: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CriteriaJson",
                table: "AssistantChatResultSets");
        }
    }
}
