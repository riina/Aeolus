using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cl.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectDirectories",
                columns: table => new
                {
                    FullPath = table.Column<string>(type: "TEXT", nullable: false),
                    RecordUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDirectories", x => x.FullPath);
                });

            migrationBuilder.CreateTable(
                name: "RecentProjects",
                columns: table => new
                {
                    FullPath = table.Column<string>(type: "TEXT", nullable: false),
                    OpenedTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecordUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProjectEvaluatorType = table.Column<string>(type: "TEXT", nullable: false),
                    Framework = table.Column<string>(type: "TEXT", nullable: false),
                    Nickname = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecentProjects", x => x.FullPath);
                });

            migrationBuilder.CreateTable(
                name: "ProjectDirectoryProjects",
                columns: table => new
                {
                    FullPath = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectDirectoryFullPath = table.Column<string>(type: "TEXT", nullable: false),
                    RecordUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProjectEvaluatorType = table.Column<string>(type: "TEXT", nullable: false),
                    Framework = table.Column<string>(type: "TEXT", nullable: false),
                    Nickname = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDirectoryProjects", x => x.FullPath);
                    table.ForeignKey(
                        name: "FK_ProjectDirectoryProjects_ProjectDirectories_ProjectDirectoryFullPath",
                        column: x => x.ProjectDirectoryFullPath,
                        principalTable: "ProjectDirectories",
                        principalColumn: "FullPath",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDirectoryProjects_ProjectDirectoryFullPath",
                table: "ProjectDirectoryProjects",
                column: "ProjectDirectoryFullPath");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectDirectoryProjects");

            migrationBuilder.DropTable(
                name: "RecentProjects");

            migrationBuilder.DropTable(
                name: "ProjectDirectories");
        }
    }
}
