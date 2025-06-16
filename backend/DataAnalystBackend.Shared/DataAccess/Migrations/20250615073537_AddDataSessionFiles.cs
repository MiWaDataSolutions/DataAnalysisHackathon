using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAnalysisHackathonBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddDataSessionFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataSessionsFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Filename = table.Column<string>(type: "text", nullable: false),
                    FileData = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataSessionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSessionsFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataSessionsFiles_DataSessions_DataSessionId",
                        column: x => x.DataSessionId,
                        principalTable: "DataSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataSessionsFiles_DataSessionId",
                table: "DataSessionsFiles",
                column: "DataSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataSessionsFiles");
        }
    }
}
