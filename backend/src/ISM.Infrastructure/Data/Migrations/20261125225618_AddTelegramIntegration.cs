using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "telegram_connections",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: true),
                    CodeHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodeExpiresAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AlertsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ThreadId = table.Column<int>(type: "int", nullable: true),
                    LastMessageAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastUpdateId = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telegram_connections", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_telegram_connections_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "telegram_polling_cursors",
                columns: table => new
                {
                    BotUsername = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Offset = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telegram_polling_cursors", x => x.BotUsername);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_telegram_connections_ChatId",
                table: "telegram_connections",
                column: "ChatId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_telegram_connections_CodeHash",
                table: "telegram_connections",
                column: "CodeHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "telegram_connections");

            migrationBuilder.DropTable(
                name: "telegram_polling_cursors");
        }
    }
}
