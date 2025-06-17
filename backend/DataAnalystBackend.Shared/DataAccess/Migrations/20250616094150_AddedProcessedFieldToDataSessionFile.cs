using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAnalysisHackathonBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddedProcessedFieldToDataSessionFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Processed",
                table: "DataSessionsFiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Processed",
                table: "DataSessionsFiles");
        }
    }
}
