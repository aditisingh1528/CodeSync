using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExecutionService.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExecutionJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Output = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorOutput = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionJobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionJobs_CreatedAt",
                table: "ExecutionJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionJobs_ProjectId",
                table: "ExecutionJobs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionJobs_Status",
                table: "ExecutionJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionJobs_UserId",
                table: "ExecutionJobs",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ExecutionJobs");
        }
    }
}
