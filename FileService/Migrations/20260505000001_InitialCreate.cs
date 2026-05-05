using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileService.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodeFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name            = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content         = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectId       = table.Column<int>(type: "int", nullable: false),
                    ParentFolderId  = table.Column<int>(type: "int", nullable: true),
                    IsFolder        = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt       = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt       = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeFiles_CodeFiles_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalTable: "CodeFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodeFiles_ParentFolderId",
                table: "CodeFiles",
                column: "ParentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeFiles_ProjectId",
                table: "CodeFiles",
                column: "ProjectId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CodeFiles");
        }
    }
}
