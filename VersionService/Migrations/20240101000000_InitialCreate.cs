using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VersionService.Migrations
{
    [Microsoft.EntityFrameworkCore.Migrations.Migration("20240101000000_InitialCreate")]
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Snapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileId          = table.Column<int>(type: "int", nullable: false),
                    Content         = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp       = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    Message         = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Snapshots_CreatedByUserId",
                table: "Snapshots",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Snapshots_FileId",
                table: "Snapshots",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Snapshots_Timestamp",
                table: "Snapshots",
                column: "Timestamp");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Snapshots");
        }
    }
}
