using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAnalysisHackathonBackend.Migrations
{
    /// <inheritdoc />
    public partial class ChangedProcessedToProcessedStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Processed",
                table: "DataSessionsFiles");

            migrationBuilder.AddColumn<int>(
                name: "ProcessedStatus",
                table: "DataSessionsFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessedStatus",
                table: "DataSessionsFiles");

            migrationBuilder.AddColumn<bool>(
                name: "Processed",
                table: "DataSessionsFiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
