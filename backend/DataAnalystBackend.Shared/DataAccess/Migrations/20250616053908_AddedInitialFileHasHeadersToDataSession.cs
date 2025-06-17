using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAnalysisHackathonBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddedInitialFileHasHeadersToDataSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InitialFileHasHeaders",
                table: "DataSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitialFileHasHeaders",
                table: "DataSessions");
        }
    }
}
