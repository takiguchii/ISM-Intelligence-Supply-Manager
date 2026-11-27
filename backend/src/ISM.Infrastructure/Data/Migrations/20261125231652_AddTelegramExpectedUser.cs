using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramExpectedUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ExpectedUserId",
                table: "telegram_connections",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedUserId",
                table: "telegram_connections");
        }
    }
}
