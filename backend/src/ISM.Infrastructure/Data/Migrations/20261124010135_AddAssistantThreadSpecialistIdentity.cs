using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssistantThreadSpecialistIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceView",
                table: "assistant_threads",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SpecialistId",
                table: "assistant_threads",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "general")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SubjectEntityType",
                table: "assistant_threads",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_assistant_threads_RestaurantId_UserId_SpecialistId_LastMessa~",
                table: "assistant_threads",
                columns: new[] { "RestaurantId", "UserId", "SpecialistId", "LastMessageAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_assistant_threads_RestaurantId_UserId_SpecialistId_LastMessa~",
                table: "assistant_threads");

            migrationBuilder.DropColumn(
                name: "SourceView",
                table: "assistant_threads");

            migrationBuilder.DropColumn(
                name: "SpecialistId",
                table: "assistant_threads");

            migrationBuilder.DropColumn(
                name: "SubjectEntityType",
                table: "assistant_threads");
        }
    }
}
