using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssistantConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assistant_threads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ParentThreadId = table.Column<int>(type: "int", nullable: true),
                    OriginAlertId = table.Column<int>(type: "int", nullable: true),
                    OriginType = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubjectEntityId = table.Column<int>(type: "int", nullable: true),
                    Subject = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContextSnapshotJson = table.Column<string>(type: "varchar(12000)", maxLength: 12000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastMessageAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsArchived = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assistant_threads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assistant_threads_restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_assistant_threads_system_alerts_OriginAlertId",
                        column: x => x.OriginAlertId,
                        principalTable: "system_alerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "assistant_messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    ThreadId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Role = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "varchar(6000)", maxLength: 6000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mode = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assistant_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assistant_messages_assistant_threads_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "assistant_threads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_assistant_messages_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_assistant_messages_RestaurantId_ThreadId_CreatedAtUtc",
                table: "assistant_messages",
                columns: new[] { "RestaurantId", "ThreadId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_assistant_messages_ThreadId",
                table: "assistant_messages",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_assistant_messages_UserId",
                table: "assistant_messages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_assistant_threads_OriginAlertId",
                table: "assistant_threads",
                column: "OriginAlertId");

            migrationBuilder.CreateIndex(
                name: "IX_assistant_threads_RestaurantId_OriginAlertId",
                table: "assistant_threads",
                columns: new[] { "RestaurantId", "OriginAlertId" });

            migrationBuilder.CreateIndex(
                name: "IX_assistant_threads_RestaurantId_UserId_LastMessageAtUtc",
                table: "assistant_threads",
                columns: new[] { "RestaurantId", "UserId", "LastMessageAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assistant_messages");

            migrationBuilder.DropTable(
                name: "assistant_threads");
        }
    }
}
