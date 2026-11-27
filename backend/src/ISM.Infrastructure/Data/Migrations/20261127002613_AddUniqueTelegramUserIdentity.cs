using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueTelegramUserIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_telegram_connections_ExpectedUserId",
                table: "telegram_connections",
                column: "ExpectedUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_telegram_connections_ExpectedUserId",
                table: "telegram_connections");
        }
    }
}
