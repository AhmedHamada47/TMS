using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TMS.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectsBoardsAndKanban : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BoardColumnId",
                table: "Tasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BoardOrder",
                table: "Tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    OrganizationId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Boards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Boards_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BoardColumns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BoardId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardColumns_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "CreatedAt", "Description", "Name", "OrganizationId" },
                values: new object[] { 1, new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc), null, "Software Development", 1 });

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BoardColumnId", "BoardOrder" },
                values: new object[] { 4, 0 });

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "BoardColumnId", "BoardOrder" },
                values: new object[] { 4, 1 });

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "BoardColumnId", "BoardOrder" },
                values: new object[] { 2, 0 });

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "BoardColumnId", "BoardOrder" },
                values: new object[] { 2, 1 });

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "BoardColumnId", "BoardOrder" },
                values: new object[] { 1, 0 });

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "BoardColumnId", "BoardOrder" },
                values: new object[] { 1, 1 });

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "BoardColumnId", "BoardOrder" },
                values: new object[] { 1, 2 });

            migrationBuilder.UpdateData(
                table: "Tasks",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "BoardColumnId", "BoardOrder" },
                values: new object[] { 2, 2 });

            migrationBuilder.InsertData(
                table: "Boards",
                columns: new[] { "Id", "CreatedAt", "Name", "ProjectId" },
                values: new object[] { 1, new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Sprint Board", 1 });

            migrationBuilder.InsertData(
                table: "BoardColumns",
                columns: new[] { "Id", "BoardId", "CreatedAt", "Name", "Order" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc), "To Do", 0 },
                    { 2, 1, new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc), "In Progress", 1 },
                    { 3, 1, new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Review", 2 },
                    { 4, 1, new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Done", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_BoardColumnId",
                table: "Tasks",
                column: "BoardColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardColumns_BoardId_Order",
                table: "BoardColumns",
                columns: new[] { "BoardId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_Boards_ProjectId",
                table: "Boards",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OrganizationId",
                table: "Projects",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_BoardColumns_BoardColumnId",
                table: "Tasks",
                column: "BoardColumnId",
                principalTable: "BoardColumns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_BoardColumns_BoardColumnId",
                table: "Tasks");

            migrationBuilder.DropTable(
                name: "BoardColumns");

            migrationBuilder.DropTable(
                name: "Boards");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_BoardColumnId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "BoardColumnId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "BoardOrder",
                table: "Tasks");
        }
    }
}
