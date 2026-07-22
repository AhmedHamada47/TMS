using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TMS.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Password = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AvatarUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tasks_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Tasks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AvatarUrl", "CreatedAt", "Email", "Name", "Password" },
                values: new object[,]
                {
                    { 1, "https://pub-a981f7fafe3c46e98d60519aae806cf8.r2.dev/Avatar/Male/Number_21_b9m4ba_elzprp.png", new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc), "ahmed@tms.com", "Ahmed Hamada", "$2a$11$im1itOCIomo1FryxxRewfOz9dCxfJ6eFBTpoOLiTK/ZAJvYtkUExm" },
                    { 2, "https://pub-a981f7fafe3c46e98d60519aae806cf8.r2.dev/Avatar/Female/Number_47_ssmlmw_zlydth.png", new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc), "sara@tms.com", "Sara Ali", "$2a$11$im1itOCIomo1FryxxRewfOz9dCxfJ6eFBTpoOLiTK/ZAJvYtkUExm" },
                    { 3, "https://pub-a981f7fafe3c46e98d60519aae806cf8.r2.dev/Avatar/Male/Number_21_b9m4ba_elzprp.png", new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc), "mohamed@tms.com", "Mohamed Hassan", "$2a$11$im1itOCIomo1FryxxRewfOz9dCxfJ6eFBTpoOLiTK/ZAJvYtkUExm" }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Color", "CreatedAt", "Description", "Name", "UserId" },
                values: new object[,]
                {
                    { 1, "#0d6efd", new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Software development tasks", "Development", 1 },
                    { 2, "#6f42c1", new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc), "UI/UX design tasks", "Design", 1 },
                    { 3, "#198754", new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Marketing and promotion tasks", "Marketing", 1 },
                    { 4, "#fd7e14", new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Meetings and coordination", "Meeting", 1 },
                    { 5, "#dc3545", new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Research and analysis tasks", "Research", 1 }
                });

            migrationBuilder.InsertData(
                table: "Tasks",
                columns: new[] { "Id", "CategoryId", "CreatedAt", "Description", "DueDate", "Priority", "Status", "Title", "UpdatedAt", "UserId" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Initialize the ASP.NET Core MVC project with proper folder structure", new DateTime(2026, 7, 14, 0, 0, 0, 0, DateTimeKind.Utc), 2, 2, "Set up project structure", null, 1 },
                    { 2, 1, new DateTime(2026, 7, 14, 0, 0, 0, 0, DateTimeKind.Utc), "Create EF Core models and configure relationships", new DateTime(2026, 7, 17, 0, 0, 0, 0, DateTimeKind.Utc), 2, 2, "Design database schema", null, 1 },
                    { 3, 2, new DateTime(2026, 7, 17, 0, 0, 0, 0, DateTimeKind.Utc), "Build an attractive dashboard with task statistics", new DateTime(2026, 7, 24, 0, 0, 0, 0, DateTimeKind.Utc), 2, 1, "Create dashboard UI", null, 2 },
                    { 4, 1, new DateTime(2026, 7, 18, 0, 0, 0, 0, DateTimeKind.Utc), "Full CRUD operations for task management", new DateTime(2026, 7, 23, 0, 0, 0, 0, DateTimeKind.Utc), 3, 1, "Implement task CRUD", null, 1 },
                    { 5, 1, new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Cover controllers and services with unit tests", new DateTime(2026, 7, 29, 0, 0, 0, 0, DateTimeKind.Utc), 1, 0, "Write unit tests", null, 3 },
                    { 6, 3, new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Plan and execute Q3 marketing campaign", new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Utc), 1, 0, "Marketing campaign Q3", null, 2 },
                    { 7, 4, new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Plan next sprint with the team", new DateTime(2026, 7, 27, 0, 0, 0, 0, DateTimeKind.Utc), 0, 0, "Sprint planning meeting", null, 1 },
                    { 8, 5, new DateTime(2026, 7, 16, 0, 0, 0, 0, DateTimeKind.Utc), "Research competitors and compile report", new DateTime(2026, 7, 25, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1, "Competitor analysis", null, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_UserId",
                table: "Categories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CategoryId",
                table: "Tasks",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CreatedAt",
                table: "Tasks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_DueDate",
                table: "Tasks",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Priority",
                table: "Tasks",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Status",
                table: "Tasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UserId",
                table: "Tasks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
